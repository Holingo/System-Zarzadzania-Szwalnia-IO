using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;
using Szwalnia.API.Domain;
using Szwalnia.API.DTO;

namespace Szwalnia.API.Services;

public class CatalogService(AppDb db)
{
    public async Task<CatalogOptions> Options(CancellationToken ct) => new(
        await db.Materials.OrderBy(x => x.Name).Select(x => new MaterialResponse(x.Id, x.Name, x.Version)).ToListAsync(ct),
        await db.Products.OrderBy(x => x.Name).ThenBy(x => x.Variant).Select(x => new ProductResponse(
            x.Id, x.Name, x.Variant, x.MaterialId, x.WidthCm, x.MetersPerSet, x.CutSetupMinutes,
            x.CutMinutesPerSet, x.CutSetupCost, x.CutCostPerSet, x.SewMinutesPerSet, x.SewCostPerSet, x.Version)).ToListAsync(ct),
        await db.Rolls.OrderBy(x => x.Name).Select(x => new RollResponse(
            x.Id, x.Name, x.MaterialId, x.WidthCm, x.RemainingMeters, x.ReservedMeters, x.RemainingMeters - x.ReservedMeters, x.Version)).ToListAsync(ct),
        await db.Seamstresses.OrderBy(x => x.Name).Select(x => new SeamstressResponse(x.Id, x.Name, x.Phone, x.Version)).ToListAsync(ct),
        await db.Batches.OrderByDescending(x => x.Id).Select(x => new BatchResponse(
            x.Id, x.CutOrderId, x.CutOrder.Product.Name, x.CutOrder.Product.Variant, x.Quantity,
            x.AllocatedQuantity, x.Quantity - x.AllocatedQuantity, x.CutOrder.SewCostPerSet, x.CutOrder.SewMinutesPerSet)).ToListAsync(ct));

    public async Task<int> SaveMaterial(int? id, string? version, MaterialRequest request)
    {
        var material = await ForSave<Material>(id, version);
        material.Name = request.Name.Trim();
        return await Save(material, id);
    }

    public async Task<int> SaveProduct(int? id, string? version, ProductRequest request)
    {
        var product = await ForSave<Product>(id, version);
        await RequireMaterial(request.MaterialId);
        product.Name = request.Name.Trim();
        product.Variant = request.Variant.Trim();
        product.MaterialId = request.MaterialId;
        product.WidthCm = request.WidthCm;
        product.MetersPerSet = request.MetersPerSet;
        product.CutSetupMinutes = request.CutSetupMinutes;
        product.CutMinutesPerSet = request.CutMinutesPerSet;
        product.CutSetupCost = request.CutSetupCost;
        product.CutCostPerSet = request.CutCostPerSet;
        product.SewMinutesPerSet = request.SewMinutesPerSet;
        product.SewCostPerSet = request.SewCostPerSet;
        return await Save(product, id);
    }

    public async Task<int> SaveRoll(int? id, string? version, RollRequest request)
    {
        var roll = await ForSave<Roll>(id, version);
        await RequireMaterial(request.MaterialId);
        Rules.Require(request.RemainingMeters >= roll.ReservedMeters, "Stan belki nie może spaść poniżej rezerwacji.");
        if (roll.MaterialId != request.MaterialId || roll.WidthCm != request.WidthCm)
        {
            Rules.Require(roll.ReservedMeters == 0, "Nie można zmienić materiału ani szerokości zarezerwowanej belki.");
        }

        roll.Name = request.Name.Trim();
        roll.MaterialId = request.MaterialId;
        roll.WidthCm = request.WidthCm;
        roll.RemainingMeters = request.RemainingMeters;
        return await Save(roll, id);
    }

    public async Task<int> SaveSeamstress(int? id, string? version, SeamstressRequest request)
    {
        var seamstress = await ForSave<Seamstress>(id, version);
        seamstress.Name = request.Name.Trim();
        seamstress.Phone = request.Phone?.Trim() ?? "";
        return await Save(seamstress, id);
    }

    public async Task Delete<T>(int id, string version) where T : Entity, new()
    {
        var entity = await ForSave<T>(id, version);
        db.Remove(entity);
        await db.SaveChangesAsync();
    }

    private async Task<T> ForSave<T>(int? id, string? version) where T : Entity, new()
    {
        if (id is null)
            return new T();

        var entity = await db.Set<T>().FindAsync(id.Value)
            ?? throw new BusinessException("Nie znaleziono rekordu.", 404);
        Rules.Version(entity, version ?? "");
        return entity;
    }

    private async Task<int> Save(Entity entity, int? id)
    {
        if (id is null)
            db.Add(entity);

        await db.SaveChangesAsync();
        return entity.Id;
    }

    private async Task RequireMaterial(int id)
    {
        if (!await db.Materials.AnyAsync(x => x.Id == id))
            throw new BusinessException("Nie znaleziono materiału.", 404);
    }
}
