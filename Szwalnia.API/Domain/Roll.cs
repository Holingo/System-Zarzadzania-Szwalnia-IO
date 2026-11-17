namespace Szwalnia.API;

public class Roll : Entity
{
    public string Name { get; set; } = "";
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public decimal WidthCm { get; set; }

    // Cały materiał pozostały na belce, również zarezerwowany
    public decimal RemainingMeters { get; set; }
    public decimal ReservedMeters { get; set; }
}