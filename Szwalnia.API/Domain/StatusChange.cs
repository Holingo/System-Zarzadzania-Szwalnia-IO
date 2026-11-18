namespace Szwalnia.API;

public class StatusChange : Entity
{
    public int? CutOrderId { get; set; }
    public CutOrder? CutOrder { get; set; }
    public int? SewOrderId { get; set; }
    public SewOrder? SewOrder { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Status { get; set; } = "";
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
}