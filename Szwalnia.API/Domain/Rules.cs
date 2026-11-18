namespace Szwalnia.API.Domain;

public class BusinessException(string message, int statusCode = 409) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
public static class Rules
{
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new BusinessException(message);
    }
    public static void Quantity(int value) => Require(value is > 0 and <= 1_000_000, "Ilość musi wynosić od 1 do 1 000 000.");
    public static decimal Cost(decimal setup, decimal unit, int quantity) => decimal.Round(setup + unit * quantity, 2, MidpointRounding.AwayFromZero);
    public static decimal Minutes(decimal setup, decimal unit, int quantity) => setup + unit * quantity;
    public static void Reserve(decimal remaining, decimal reserved, decimal requested) => Require(requested >= 0 && requested <= remaining - reserved, "Za mało dostępnego materiału.");
    public static void Allocate(int total, int allocated, int requested) { Quantity(requested); Require(requested <= total - allocated, "Za mało dostępnych kompletów w partii."); }
    public static void Transition(CutStatus from, CutStatus to) => Require((from, to) is (CutStatus.Queued, CutStatus.Cutting) or (CutStatus.Cutting, CutStatus.Cut), "Niedozwolona zmiana statusu krojenia.");
    public static void Transition(SewStatus from, SewStatus to) => Require((from, to) is (SewStatus.New, SewStatus.Sewing) or (SewStatus.Sewing, SewStatus.Sewn), "Niedozwolona zmiana statusu szycia.");
    public static void Version(Entity entity, string version)
    {
        Require(Convert.ToBase64String(entity.Version) == version, "Dane zostały zmienione przez innego pracownika. Odśwież listę.");
    }
    public static (DateTimeOffset Start, DateTimeOffset End) WeekRange(DateOnly monday)
    {
        Require(monday.Year is >= 2000 and <= 2100, "Raport wymaga daty z lat 2000–2100.");
        Require(monday.DayOfWeek == DayOfWeek.Monday, "Początek tygodnia musi być poniedziałkiem.");
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        var start = monday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var end = monday.AddDays(7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return (new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, zone)), new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(end, zone)));
    }
}
