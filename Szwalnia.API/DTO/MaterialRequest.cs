using System.ComponentModel.DataAnnotations;

namespace Szwalnia.API.DTO;

public record MaterialRequest(
    [Required, StringLength(150)]
    string Name
);