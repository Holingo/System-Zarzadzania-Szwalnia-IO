using System.Text.Json.Serialization;

namespace Szwalnia.API.DTO;

public record CutResponse(int Id, string Product, string Variant, string Roll,
    int Quantity, DateOnly DueDate, [property: JsonConverter(typeof(JsonStringEnumConverter<CutStatus>))] CutStatus Status,
    decimal EstimatedCost, decimal EstimatedMinutes, decimal EstimatedMeters, int? ActualQuantity,
    decimal? ActualMeters, byte[] Version);
public record SewResponse(int Id, string Product, string Variant, string Seamstress, int SeamstressId,
    int Quantity, DateOnly DueDate, [property: JsonConverter(typeof(JsonStringEnumConverter<SewStatus>))] SewStatus Status,
    decimal EstimatedCost, decimal EstimatedMinutes, byte[] Version);
public record HistoryResponse(string Status, DateTimeOffset ChangedAt, string User);
public record DashboardResponse(int Queued, int Cutting, long AvailableSets, int Sewing, int Overdue);
