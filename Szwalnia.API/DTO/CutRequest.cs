using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record CutRequest(
    [Range(1, int.MaxValue)]
    int ProductId,

    [Range(1, int.MaxValue)]
    int RollId,

    [Range(1, 1000000)]
    int Quantity,

    DateOnly DueDate
);