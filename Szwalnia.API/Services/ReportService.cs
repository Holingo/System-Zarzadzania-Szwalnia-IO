using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;
using Szwalnia.API.Domain;
using Szwalnia.API.DTO;

namespace Szwalnia.API.Services;

public class ReportService(AppDb db)
{
    public async Task<ReportSummary> Queue(DateOnly week, int userId, CancellationToken ct)
    {
        Rules.WeekRange(week);
        var job = new ReportJob { WeekStart = week, UserId = userId };
        db.Reports.Add(job);
        await db.SaveChangesAsync(ct);
        return new ReportSummary(job.Id, job.WeekStart, job.Status, job.Error);
    }

    public Task<List<ReportSummary>> List(int userId, bool admin, CancellationToken ct) =>
        db.Reports.Where(x => admin || x.UserId == userId)
            .OrderByDescending(x => x.Id).Take(100)
            .Select(x => new ReportSummary(x.Id, x.WeekStart, x.Status, x.Error)).ToListAsync(ct);

    public async Task<ReportResponse> Get(int id, int userId, bool admin, CancellationToken ct)
    {
        var job = await db.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && (admin || x.UserId == userId), ct)
            ?? throw new BusinessException("Nie znaleziono raportu.", 404);
        return new ReportResponse(job.Id, job.WeekStart, job.Status, job.Error,
            job.ResultJson is null ? null : JsonSerializer.Deserialize<JsonElement>(job.ResultJson));
    }

    public async Task<WeeklyReport> Generate(DateOnly week, CancellationToken ct)
    {
        var (start, end) = Rules.WeekRange(week);
        var cuts = await db.Cuts.AsNoTracking().Where(x => x.CompletedAt >= start && x.CompletedAt < end)
            .OrderBy(x => x.CompletedAt).ThenBy(x => x.Id)
            .Select(x => new CutReportRow(x.Id, x.Product.Name, x.Product.Variant, x.ActualQuantity ?? 0,
                x.ActualMeters ?? 0, Rules.Cost(x.SetupCost, x.CostPerSet, x.ActualQuantity ?? 0))).ToListAsync(ct);
        var sews = await db.Sews.AsNoTracking().Where(x => x.CompletedAt >= start && x.CompletedAt < end)
            .OrderBy(x => x.CompletedAt).ThenBy(x => x.Id)
            .Select(x => new SewReportRow(x.Id, x.Batch.CutOrder.Product.Name, x.Seamstress.Name,
                x.Quantity, Rules.Cost(0, x.CostPerSet, x.Quantity))).ToListAsync(ct);

        return new WeeklyReport(week, week.AddDays(6), cuts.Sum(x => (long)x.Quantity), sews.Sum(x => (long)x.Quantity),
            cuts.Sum(x => x.Cost), sews.Sum(x => x.Cost), cuts.Sum(x => x.Meters), cuts, sews);
    }
}
