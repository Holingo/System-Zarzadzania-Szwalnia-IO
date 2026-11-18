using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record StatusRequest(
    [Required]
    string Status,

    [Required]
    string Version
);