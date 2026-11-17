namespace Szwalnia.API;

public class Product : Entity
{
    public string Name { get; set; } = "";
    public string Variant { get; set; } = "";
    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    
    // Szerokość materiału
    public decimal WidthCm { get; set; }
    
    public decimal MetersPerSet { get; set; }
    public decimal CutSetupMinutes { get; set; }
    public decimal CutMinutesPerSet { get; set; }
    public decimal CutSetupCost { get; set; }
    public decimal CutCostPerSet { get; set; }
    public decimal SewMinutesPerSet { get; set; }
    public decimal SewCostPerSet { get; set; }
}