using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record SewEditRequest(
    [Range(1, int.MaxValue)]
    int SeamstressId,

    [Range(1, 1000000)]
    int Quantity,

    DateOnly DueDate,

    [Required]
    string Version
);