namespace Szwalnia.API;

public enum CutStatus
{
    Queued = 0,
    Cutting = 1,
    Cut = 2
}

public class CutOrder : Entity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int RollId { get; set; }
    public Roll Roll { get; set; } = null!;
    public int Quantity { get; set; }
    public CutStatus Status { get; set; }
    public DateOnly DueDate { get; set; }

    // Kopia norm i stawek obowiązujących przy utworzeniu zlecenia.
    public decimal MetersPerSet { get; set; }
    public decimal SetupMinutes { get; set; }
    public decimal MinutesPerSet { get; set; }
    public decimal SetupCost { get; set; }
    public decimal CostPerSet { get; set; }
    public decimal SewMinutesPerSet { get; set; }
    public decimal SewCostPerSet { get; set; }

    // Wynik wykonania — uzupełniany przy zakończeniu krojenia.
    public int? ActualQuantity { get; set; }
    public decimal? ActualMeters { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}