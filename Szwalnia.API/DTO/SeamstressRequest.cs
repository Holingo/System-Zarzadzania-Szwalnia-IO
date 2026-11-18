using System.ComponentModel.DataAnnotations;

namespace Szwalnia.Api.DTO;

public record SeamstressRequest(
    [Required, StringLength(150)]
    string Name,
    
    [StringLength(100)]
    string Phone
);
