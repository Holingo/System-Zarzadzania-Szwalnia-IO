using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record SeamstressRequest(
    [Required, StringLength(150)]
    string Name,
    
    [StringLength(100)]
    string? Phone
);
