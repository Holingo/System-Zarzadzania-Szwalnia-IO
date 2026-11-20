using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;
using Szwalnia.API.Domain;
using Szwalnia.API.DTO;

namespace Szwalnia.API.Services;

public class ProductionQueries(AppDb db)
{
    private const int PageSize = 20;

    private static readonly Expression<Func<CutOrder, CutResponse>> CutRow = x => new(
        x.Id, x.Product.Name, x.Product.Variant, x.Roll.Name, x.Quantity, x.DueDate, x.Status,
        Rules.Cost(x.SetupCost, x.CostPerSet, x.Quantity), Rules.Minutes(x.SetupMinutes, x.MinutesPerSet, x.Quantity),
        x.MetersPerSet * x.Quantity, x.ActualQuantity, x.ActualMeters, x.Version);

    private static readonly Expression<Func<SewOrder, SewResponse>> SewRow = x => new(
        x.Id, x.Batch.CutOrder.Product.Name, x.Batch.CutOrder.Product.Variant, x.Seamstress.Name,
        x.SeamstressId, x.Quantity, x.DueDate, x.Status, Rules.Cost(0, x.CostPerSet, x.Quantity),
        Rules.Minutes(0, x.MinutesPerSet, x.Quantity), x.Version);

    public async Task<Page<CutResponse>> Cuts(int page, string? search, string? status, CancellationToken ct)
    {
        var query = db.Cuts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Product.Name.Contains(term) || x.Product.Variant.Contains(term) || x.Roll.Name.Contains(term));
        }
        if (FilterStatus<CutStatus>(status) is { } cutStatus)
            query = query.Where(x => x.Status == cutStatus);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.DueDate).ThenBy(x => x.Id)
            .Skip((page - 1) * PageSize).Take(PageSize).Select(CutRow).ToListAsync(ct);
        return new Page<CutResponse>(items, total, page, PageSize);
    }

    public async Task<Page<SewResponse>> Sews(int page, string? search, string? status, int? seamstressId, CancellationToken ct)
    {
        var query = db.Sews.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Batch.CutOrder.Product.Name.Contains(term) ||
                x.Batch.CutOrder.Product.Variant.Contains(term) || x.Seamstress.Name.Contains(term));
        }
        if (FilterStatus<SewStatus>(status) is { } sewStatus)
            query = query.Where(x => x.Status == sewStatus);
        if (seamstressId is not null)
            query = query.Where(x => x.SeamstressId == seamstressId);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.DueDate).ThenBy(x => x.Id)
            .Skip((page - 1) * PageSize).Take(PageSize).Select(SewRow).ToListAsync(ct);
        return new Page<SewResponse>(items, total, page, PageSize);
    }

    public async Task<CutResponse> Cut(int id, CancellationToken ct) =>
        await db.Cuts.Where(x => x.Id == id).Select(CutRow).SingleOrDefaultAsync(ct)
        ?? throw new BusinessException("Nie znaleziono zlecenia krojenia.", 404);

    public async Task<SewResponse> Sew(int id, CancellationToken ct) =>
        await db.Sews.Where(x => x.Id == id).Select(SewRow).SingleOrDefaultAsync(ct)
        ?? throw new BusinessException("Nie znaleziono zlecenia szycia.", 404);

    public async Task<List<HistoryResponse>> History(string resource, int id, CancellationToken ct)
    {
        var query = resource switch
        {
            "cuts" when await db.Cuts.AnyAsync(x => x.Id == id, ct) => db.StatusChanges.Where(x => x.CutOrderId == id),
            "sews" when await db.Sews.AnyAsync(x => x.Id == id, ct) => db.StatusChanges.Where(x => x.SewOrderId == id),
            _ => throw new BusinessException("Nie znaleziono zlecenia.", 404)
        };
        return await query.OrderBy(x => x.ChangedAt).ThenBy(x => x.Id)
            .Select(x => new HistoryResponse(x.Status, x.ChangedAt, x.User.Name)).ToListAsync(ct);
    }

    public async Task<DashboardResponse> Dashboard(CancellationToken ct)
    {
        var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Europe/Warsaw");
        var today = DateOnly.FromDateTime(now.DateTime);
        return new DashboardResponse(
            await db.Cuts.CountAsync(x => x.Status == CutStatus.Queued, ct),
            await db.Cuts.CountAsync(x => x.Status == CutStatus.Cutting, ct),
            await db.Batches.SumAsync(x => (long)x.Quantity - x.AllocatedQuantity, ct),
            await db.Sews.CountAsync(x => x.Status == SewStatus.Sewing, ct),
            await db.Sews.CountAsync(x => x.DueDate < today && x.Status != SewStatus.Sewn, ct));
    }

    private static T? FilterStatus<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Enum.TryParse<T>(value, true, out var status) && Enum.IsDefined(status))
            return status;

        throw new BusinessException("Niepoprawny filtr statusu.", 400);
    }
}
