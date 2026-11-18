using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record ProductRequest(
    [Required, StringLength(150)]
    string Name,

    [Required, StringLength(100)]
    string Variant,

    [Range(1, int.MaxValue)]
    int MaterialId,

    [Range(1, 1000)]
    decimal WidthCm,

    [Range(typeof(decimal), "0.0001", "10000")]
    decimal MetersPerSet,

    [Range(0, 100000)]
    decimal CutSetupMinutes,

    [Range(0, 100000)]
    decimal CutMinutesPerSet,

    [Range(0, 1000000)]
    decimal CutSetupCost,

    [Range(0, 1000000)]
    decimal CutCostPerSet,

    [Range(0, 100000)]
    decimal SewMinutesPerSet,

    [Range(0, 1000000)]
    decimal SewCostPerSet
);