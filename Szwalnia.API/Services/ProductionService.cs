using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;
using Szwalnia.API.Domain;
using Szwalnia.API.DTO;

namespace Szwalnia.API.Services;

public class ProductionService(AppDb db)
{
    private static void Due(DateOnly date)
    {
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                DateTimeOffset.UtcNow,
                "Europe/Warsaw"
            ).DateTime
        );

        Rules.Require(
            date >= today,
            "Termin nie może być w przeszłości."
        );
    }
    
    public async Task<CutOrder> CreateCut(CutRequest r, int userId)
    {
        Rules.Quantity(r.Quantity); Due(r.DueDate);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == r.ProductId) ?? throw new BusinessException("Nie znaleziono produktu.", 404);
        var roll = await db.Rolls.SingleOrDefaultAsync(x => x.Id == r.RollId) ?? throw new BusinessException("Nie znaleziono belki.", 404);
        Rules.Require(product.MaterialId == roll.MaterialId && product.WidthCm == roll.WidthCm, "Materiał i szerokość belki muszą odpowiadać normie produktu.");
        var meters = product.MetersPerSet * r.Quantity;
        Rules.Reserve(roll.RemainingMeters, roll.ReservedMeters, meters);
        roll.ReservedMeters += meters;
        var cut = new CutOrder { ProductId = product.Id, RollId = roll.Id, Quantity = r.Quantity, DueDate = r.DueDate,
            MetersPerSet = product.MetersPerSet, SetupMinutes = product.CutSetupMinutes, MinutesPerSet = product.CutMinutesPerSet,
            SetupCost = product.CutSetupCost, CostPerSet = product.CutCostPerSet, SewCostPerSet = product.SewCostPerSet, SewMinutesPerSet = product.SewMinutesPerSet };
        db.Cuts.Add(cut); db.StatusChanges.Add(new StatusChange { CutOrder = cut, UserId = userId, Status = "Queued" });
        await db.SaveChangesAsync(); return cut;
    }
    
    public async Task EditCut(int id, CutEditRequest r)
    {
        var cut = await GetCut(id); Rules.Version(cut, r.Version);
        Rules.Require(cut.Status == CutStatus.Queued, "Edytować można tylko zlecenie w kolejce.");
        Rules.Quantity(r.Quantity); Due(r.DueDate);
        var roll = cut.Roll; var old = cut.Quantity * cut.MetersPerSet; var next = r.Quantity * cut.MetersPerSet;
        Rules.Reserve(roll.RemainingMeters, roll.ReservedMeters - old, next);
        roll.ReservedMeters += next - old; cut.Quantity = r.Quantity; cut.DueDate = r.DueDate;
        await db.SaveChangesAsync();
    }
    
    public async Task ChangeCut(int id, CutStatusRequest r, int userId)
    {
        var cut = await GetCut(id); Rules.Version(cut, r.Version);
        Rules.Require(Enum.TryParse<CutStatus>(r.Status, out var next) && Enum.IsDefined(next), "Niepoprawny status."); Rules.Transition(cut.Status, next);
        if (next == CutStatus.Cut)
        {
            Rules.Require(r.ActualQuantity.HasValue && r.ActualMeters.HasValue, "Podaj rzeczywistą ilość i zużycie materiału.");
            Rules.Require(r.ActualQuantity is >= 0 and <= 1000000 && r.ActualMeters >= 0, "Niepoprawne wartości wykonania.");
            Rules.Require(r.ActualQuantity == 0 || r.ActualMeters > 0, "Dodatnia ilość wymaga dodatniego zużycia materiału.");
            var roll = cut.Roll; var reservation = cut.Quantity * cut.MetersPerSet;
            Rules.Reserve(roll.RemainingMeters, roll.ReservedMeters - reservation, r.ActualMeters!.Value);
            roll.ReservedMeters -= reservation; roll.RemainingMeters -= r.ActualMeters.Value;
            cut.ActualQuantity = r.ActualQuantity; cut.ActualMeters = r.ActualMeters; cut.CompletedAt = DateTimeOffset.UtcNow;
            db.Batches.Add(new Batch { CutOrderId = cut.Id, Quantity = r.ActualQuantity!.Value });
        }
        cut.Status = next; db.StatusChanges.Add(new StatusChange { CutOrderId = cut.Id, UserId = userId, Status = next.ToString() });
        await db.SaveChangesAsync();
    }
    
    public async Task DeleteCut(int id, string version)
    {
        var cut = await GetCut(id); Rules.Version(cut, version);
        Rules.Require(cut.Status == CutStatus.Queued, "Usunąć można tylko zlecenie w kolejce.");
        cut.Roll.ReservedMeters -= cut.Quantity * cut.MetersPerSet;
        db.StatusChanges.RemoveRange(await db.StatusChanges.Where(x => x.CutOrderId == id).ToListAsync()); db.Cuts.Remove(cut);
        await db.SaveChangesAsync();
    }
    
    public async Task<SewOrder> CreateSew(SewRequest r, int userId)
    {
        Due(r.DueDate);
        var batch = await db.Batches.Include(x => x.CutOrder).SingleOrDefaultAsync(x => x.Id == r.BatchId) ?? throw new BusinessException("Nie znaleziono partii.", 404);
        Rules.Require(batch.CutOrder.Status == CutStatus.Cut, "Partia nie jest gotowa do szycia.");
        Rules.Allocate(batch.Quantity, batch.AllocatedQuantity, r.Quantity);
        Rules.Require(await db.Seamstresses.AnyAsync(x => x.Id == r.SeamstressId), "Nie znaleziono chałupniczki.");
        batch.AllocatedQuantity += r.Quantity;
        var sew = new SewOrder { BatchId = batch.Id, SeamstressId = r.SeamstressId, Quantity = r.Quantity, DueDate = r.DueDate, CostPerSet = batch.CutOrder.SewCostPerSet, MinutesPerSet = batch.CutOrder.SewMinutesPerSet };
        db.Sews.Add(sew); db.StatusChanges.Add(new StatusChange { SewOrder = sew, UserId = userId, Status = "New" });
        await db.SaveChangesAsync(); return sew;
    }
    
    public async Task EditSew(int id, SewEditRequest r)
    {
        var sew = await GetSew(id); Rules.Version(sew, r.Version);
        Rules.Require(sew.Status == SewStatus.New, "Edytować można tylko nowe zlecenie szycia."); Due(r.DueDate);
        Rules.Allocate(sew.Batch.Quantity, sew.Batch.AllocatedQuantity - sew.Quantity, r.Quantity);
        Rules.Require(await db.Seamstresses.AnyAsync(x => x.Id == r.SeamstressId), "Nie znaleziono chałupniczki.");
        sew.Batch.AllocatedQuantity += r.Quantity - sew.Quantity; sew.Quantity = r.Quantity; sew.SeamstressId = r.SeamstressId; sew.DueDate = r.DueDate;
        await db.SaveChangesAsync();
    }
    
    public async Task ChangeSew(int id, StatusRequest r, int userId)
    {
        var sew = await GetSew(id); Rules.Version(sew, r.Version);
        Rules.Require(Enum.TryParse<SewStatus>(r.Status, out var next) && Enum.IsDefined(next), "Niepoprawny status."); Rules.Transition(sew.Status, next);
        sew.Status = next; if (next == SewStatus.Sewn) sew.CompletedAt = DateTimeOffset.UtcNow;
        db.StatusChanges.Add(new StatusChange { SewOrderId = id, UserId = userId, Status = next.ToString() }); await db.SaveChangesAsync();
    }
    
    public async Task DeleteSew(int id, string version)
    {
        var sew = await GetSew(id); Rules.Version(sew, version); Rules.Require(sew.Status == SewStatus.New, "Usunąć można tylko nowe zlecenie szycia.");
        sew.Batch.AllocatedQuantity -= sew.Quantity;
        db.StatusChanges.RemoveRange(await db.StatusChanges.Where(x => x.SewOrderId == id).ToListAsync()); db.Sews.Remove(sew); await db.SaveChangesAsync();
    }
    
    private async Task<CutOrder> GetCut(int id) => await db.Cuts.Include(x => x.Roll).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Nie znaleziono zlecenia.", 404);
    private async Task<SewOrder> GetSew(int id) => await db.Sews.Include(x => x.Batch).SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Nie znaleziono zlecenia.", 404);
}