namespace Szwalnia.API.DTO;

public record Page<T>(
    IReadOnlyList<T> Items,
    int Total,
    int PageNumber,
    int PageSize
);