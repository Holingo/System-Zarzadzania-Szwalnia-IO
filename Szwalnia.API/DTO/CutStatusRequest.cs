using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record CutStatusRequest(
    [Required]
    string Status,

    [Required]
    string Version,

    [Range(0, 1000000)]
    int? ActualQuantity,

    [Range(0, 1000000)]
    decimal? ActualMeters
);