using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Szwalnia.Tests;

public sealed partial class SqlApiTests
{
    [SqlFact]
    public async Task LoginCreatesSessionAndLogoutRemovesIt()
    {
        using var client = Client();
        using var anonymous = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var wrong = await client.PostAsJsonAsync("/api/auth/login", new { name = "admin", password = "incorrect" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);

        await Login(client, "admin");
        Assert.Equal("Admin", (await Get(client, "/api/auth/me")).GetProperty("role").GetString());

        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var signedOut = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
    }

    [SqlFact]
    public async Task WorkersReceiveAccessOnlyToTheirProductionStage()
    {
        using var cutter = Client();
        using var sewer = Client();
        await Login(cutter, "worker");
        await Login(sewer, "sewing");

        using var cuttingAllowed = await cutter.GetAsync("/api/cuts");
        using var cuttingForbidden = await sewer.GetAsync("/api/cuts");
        using var sewingAllowed = await sewer.GetAsync("/api/sews");
        using var sewingForbidden = await cutter.GetAsync("/api/sews");
        using var reportsForbidden = await sewer.GetAsync("/api/reports");
        Assert.Equal(HttpStatusCode.OK, cuttingAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cuttingForbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sewingAllowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sewingForbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, reportsForbidden.StatusCode);
    }

    [SqlFact]
    public async Task CutCrudReservesMaterialRejectsStaleUpdatesAndReleasesReservationOnDelete()
    {
        using var client = Client();
        await Login(client, "admin");
        var catalog = await Get(client, "/api/catalog/options");
        int rollId = FirstId(catalog, "rolls");
        int productId = FirstId(catalog, "products");
        var dueDate = WarsawDate().AddDays(1);
        using var oversized = await client.PostAsJsonAsync("/api/cuts", new { productId, rollId, quantity = 100, dueDate });
        Assert.Equal(HttpStatusCode.Conflict, oversized.StatusCode);

        int id = await Create(client, "/api/cuts", new { productId, rollId, quantity = 10, dueDate });
        var roll = (await Get(client, "/api/catalog/options")).GetProperty("rolls")[0];
        Assert.Equal(12m, roll.GetProperty("reservedMeters").GetDecimal());
        var cut = await Get(client, $"/api/cuts/{id}");
        string version = Version(cut);
        using var edited = await client.PutAsJsonAsync($"/api/cuts/{id}", new { quantity = 15, dueDate, version });
        Assert.Equal(HttpStatusCode.NoContent, edited.StatusCode);
        using var stale = await client.PutAsJsonAsync($"/api/cuts/{id}", new { quantity = 14, dueDate, version });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var filtered = await Get(client, "/api/cuts?page=1&search=Fartuch&status=Queued");
        Assert.Equal(id, Assert.Single(filtered.GetProperty("items").EnumerateArray()).GetProperty("id").GetInt32());
        cut = await Get(client, $"/api/cuts/{id}");
        Assert.Equal(15, cut.GetProperty("quantity").GetInt32());
        using var deleted = await client.DeleteAsync($"/api/cuts/{id}?version={Uri.EscapeDataString(Version(cut))}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        roll = (await Get(client, "/api/catalog/options")).GetProperty("rolls")[0];
        Assert.Equal(0m, roll.GetProperty("reservedMeters").GetDecimal());
    }
}
