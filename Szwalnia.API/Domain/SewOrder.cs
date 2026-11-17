namespace Szwalnia.API;

public class SewOrder : Entity
{
    public class SewOrder : Entity
    {
        public int BatchId { get; set; }
        public Batch Batch { get; set; } = null!;
        public int SeamstressId { get; set; }
        public Seamstress Seamstress { get; set; } = null!;
        public int Quantity { get; set; }
        public DateOnly DueDate { get; set; }
        public SewStatus Status { get; set; }
        public decimal CostPerSet { get; set; }
        public decimal MinutesPerSet { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}