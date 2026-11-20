using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Data;

namespace Szwalnia.API.Services;

public class ReportWorker(IServiceScopeFactory scopes, ILogger<ReportWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await RunNext(stoppingToken))
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Nie udało się przetworzyć kolejki raportów.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task<bool> RunNext(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var now = DateTimeOffset.UtcNow;
        var job = await db.Reports
            .Where(x => x.Status == "Pending" || (x.Status == "Running" && x.LeaseUntil <= now))
            .OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (job is null)
            return false;

        job.Status = "Running";
        job.LeaseToken = Guid.NewGuid();
        job.LeaseUntil = now.AddMinutes(5);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return true; // Inny worker przejął to zadanie.
        }

        try
        {
            var reports = scope.ServiceProvider.GetRequiredService<ReportService>();
            job.ResultJson = JsonSerializer.Serialize(await reports.Generate(job.WeekStart, ct), JsonOptions);
            job.Status = "Completed";
            job.Error = null;
        }
        catch (Exception exception) when (!ct.IsCancellationRequested)
        {
            logger.LogError(exception, "Generowanie raportu {ReportId} nie powiodło się.", job.Id);
            job.ResultJson = null;
            job.Error = "Generowanie raportu nie powiodło się. Utwórz raport ponownie.";
            job.Status = "Failed";
        }

        job.LeaseToken = null;
        job.LeaseUntil = null;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Wygasłą dzierżawę przejął inny worker. Jego wynik pozostaje w bazie.
        }
        return true;
    }
}
