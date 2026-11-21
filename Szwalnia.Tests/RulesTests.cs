using Szwalnia.API;
using Szwalnia.API.Domain;
using Xunit;

namespace Szwalnia.Tests;

public class RulesTests
{
    [Fact]
    public void ReservationAllowsExactlyTheAvailableMeters()
        => Rules.Reserve(100m, 40m, 60m);

    [Fact]
    public void ReservationRejectsMetersReservedByAnotherOrder()
        => Assert.Throws<BusinessException>(() => Rules.Reserve(100m, 40m, 60.0001m));

    [Fact]
    public void AllocationRejectsMoreSetsThanRemainInTheBatch()
        => Assert.Throws<BusinessException>(() => Rules.Allocate(12, 9, 4));

    [Fact]
    public void AllocationAllowsExactlyTheRemainingSets()
        => Rules.Allocate(12, 9, 3);

    [Fact]
    public void CuttingCannotSkipTheInProgressStatus()
        => Assert.Throws<BusinessException>(() => Rules.Transition(CutStatus.Queued, CutStatus.Cut));

    [Fact]
    public void SewingCannotReopenACompletedOrder()
        => Assert.Throws<BusinessException>(() => Rules.Transition(SewStatus.Sewn, SewStatus.Sewing));

    [Fact]
    public void CostRoundsHalfAGroszAwayFromZero()
        => Assert.Equal(10.01m, Rules.Cost(10m, 0.005m, 1));

    [Fact]
    public void ReportRequiresAMonday()
        => Assert.Throws<BusinessException>(() => Rules.WeekRange(new DateOnly(2026, 3, 24)));

    [Fact]
    public void ReportWeekUsesWarsawBoundariesAcrossDaylightSavingChange()
    {
        var (start, end) = Rules.WeekRange(new DateOnly(2026, 3, 23));
        Assert.Equal(new DateTimeOffset(2026, 3, 22, 23, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 3, 29, 22, 0, 0, TimeSpan.Zero), end);
    }

    [Fact]
    public void StaleRecordVersionReturnsAConflict()
    {
        var material = new Material { Version = [1, 2] };
        var error = Assert.Throws<BusinessException>(() => Rules.Version(material, Convert.ToBase64String([1, 1])));
        Assert.Equal(409, error.StatusCode);
    }
}
