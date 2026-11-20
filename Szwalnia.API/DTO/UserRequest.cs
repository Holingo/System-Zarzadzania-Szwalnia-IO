using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record UserRequest(
    [Required, StringLength(100, MinimumLength = 3)]
    string Name,
    
    [Required, StringLength(200, MinimumLength = 12)]
    string Password,
    
    [Required, RegularExpression(UserRole.AllPattern)]
    string Role
);
