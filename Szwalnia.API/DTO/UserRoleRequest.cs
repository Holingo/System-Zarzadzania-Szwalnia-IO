using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record UserRoleRequest(
    [Required, RegularExpression(UserRole.ProductionPattern)] string Role,
    [Required] string Version
);
