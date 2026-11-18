using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record LoginRequest(
    [Required, StringLength(100)]
    string Name,
    
    [Required, StringLength(200)]
    string Password
);