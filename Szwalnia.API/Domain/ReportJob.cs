namespace Szwalnia.API;

public class ReportJob : Entity
{
    public DateOnly WeekStart { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public string? ResultJson { get; set; }
    public string? Error { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}