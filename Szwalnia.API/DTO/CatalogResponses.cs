namespace Szwalnia.API.DTO;

public record MaterialResponse(int Id, string Name, byte[] Version);
public record ProductResponse(int Id, string Name, string Variant, int MaterialId, decimal WidthCm,
    decimal MetersPerSet, decimal CutSetupMinutes, decimal CutMinutesPerSet, decimal CutSetupCost,
    decimal CutCostPerSet, decimal SewMinutesPerSet, decimal SewCostPerSet, byte[] Version);
public record RollResponse(int Id, string Name, int MaterialId, decimal WidthCm,
    decimal RemainingMeters, decimal ReservedMeters, decimal AvailableMeters, byte[] Version);
public record SeamstressResponse(int Id, string Name, string Phone, byte[] Version);
public record BatchResponse(int Id, int CutOrderId, string Product, string Variant, int Quantity,
    int AllocatedQuantity, int Available, decimal SewCostPerSet, decimal SewMinutesPerSet);
public record CatalogOptions(IReadOnlyList<MaterialResponse> Materials, IReadOnlyList<ProductResponse> Products,
    IReadOnlyList<RollResponse> Rolls, IReadOnlyList<SeamstressResponse> Seamstresses, IReadOnlyList<BatchResponse> Batches);
