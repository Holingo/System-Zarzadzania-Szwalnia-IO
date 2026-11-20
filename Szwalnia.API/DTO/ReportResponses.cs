using System.Text.Json;

namespace Szwalnia.API.DTO;

public record ReportSummary(int Id, DateOnly WeekStart, string Status, string? Error);
public record ReportResponse(int Id, DateOnly WeekStart, string Status, string? Error, JsonElement? Result);
public record CutReportRow(int Id, string Product, string Variant, int Quantity, decimal Meters, decimal Cost);
public record SewReportRow(int Id, string Product, string Seamstress, int Quantity, decimal Cost);

public record WeeklyReport(DateOnly WeekStart, DateOnly WeekEnd, long CutQuantity, long SewnQuantity,
    decimal CuttingCost, decimal SewingCost, decimal MaterialMeters,
    IReadOnlyList<CutReportRow> Cuts, IReadOnlyList<SewReportRow> Sews);
