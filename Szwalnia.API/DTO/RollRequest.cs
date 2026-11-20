using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record RollRequest(
    [Required, StringLength(150)] string Name,
    [Range(1, int.MaxValue)] int MaterialId,
    [Range(1, 1000)] decimal WidthCm,
    [Range(0, 1000000)] decimal RemainingMeters
);
