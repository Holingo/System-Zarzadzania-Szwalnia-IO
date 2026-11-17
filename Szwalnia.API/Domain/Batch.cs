namespace Szwalnia.API;

public class Batch : Entity
{
    public int CutOrderId { get; set; }
    public CutOrder CutOrder { get; set; } = null!;
    public int Quantity { get; set; }
    public int AllocatedQuantity { get; set; }
}