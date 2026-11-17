namespace Szwalnia.API;

public abstract class Entity
{
    public int Id { get; set; }

    [Timestamp] 
    public byte[] Version { get; set; } = [];
}