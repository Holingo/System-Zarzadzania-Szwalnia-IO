using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record CutEditRequest(
    [Range(1, 1000000)]
    int Quantity,

    DateOnly DueDate,

    [Required]
    string Version
);