using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Szwalnia.Tests;

public sealed partial class SqlApiTests
{
    [SqlFact]
    public async Task CompletingProductionMakesItsTotalsAvailableInTheBackgroundReport()
    {
        using var client = Client();
        await Login(client, "admin");
        var catalog = await Get(client, "/api/catalog/options");
        int productId = FirstId(catalog, "products");
        int rollId = FirstId(catalog, "rolls");
        int seamstressId = FirstId(catalog, "seamstresses");
        var today = WarsawDate();
        var dueDate = today.AddDays(1);

        int cutId = await Create(client, "/api/cuts", new { productId, rollId, quantity = 10, dueDate });
        var cutPath = $"/api/cuts/{cutId}";
        await SetStatus(client, cutPath, "Cutting");
        await SetStatus(client, cutPath, "Cut", actualQuantity: 8, actualMeters: 11m);
        catalog = await Get(client, "/api/catalog/options");
        int batchId = FirstId(catalog, "batches");
        using var oversized = await client.PostAsJsonAsync("/api/sews", new { batchId, seamstressId, quantity = 9, dueDate });
        Assert.Equal(HttpStatusCode.Conflict, oversized.StatusCode);

        int sewId = await Create(client, "/api/sews", new { batchId, seamstressId, quantity = 6, dueDate });
        var sewPath = $"/api/sews/{sewId}";
        await SetStatus(client, sewPath, "Sewing");
        await SetStatus(client, sewPath, "Sewn");
        var history = await Get(client, $"/api/history/sews/{sewId}");
        Assert.Equal(new[] { "New", "Sewing", "Sewn" }, history.EnumerateArray().Select(item => item.GetProperty("status").GetString()));

        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        using var queued = await client.PostAsJsonAsync("/api/reports", new { weekStart });
        Assert.Equal(HttpStatusCode.Accepted, queued.StatusCode);
        var location = queued.Headers.Location;
        Assert.NotNull(location);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        while (true)
        {
            var job = await Get(client, location.ToString(), timeout.Token);
            var status = job.GetProperty("status").GetString();
            Assert.NotEqual("Failed", status);
            if (status == "Completed")
            {
                var result = job.GetProperty("result");
                Assert.Equal(8, result.GetProperty("cutQuantity").GetInt64());
                Assert.Equal(6, result.GetProperty("sewnQuantity").GetInt64());
                Assert.Equal(11m, result.GetProperty("materialMeters").GetDecimal());
                Assert.Equal(44m, result.GetProperty("cuttingCost").GetDecimal());
                Assert.Equal(108m, result.GetProperty("sewingCost").GetDecimal());
                return;
            }
            await Task.Delay(250, timeout.Token);
        }
    }
}
