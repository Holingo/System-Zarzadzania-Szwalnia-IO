using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Szwalnia.API.Data;
using Xunit;

namespace Szwalnia.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SZWALNIA_TEST_SQL")))
            Skip = "Ustaw SZWALNIA_TEST_SQL, aby uruchomić testy z SQL Serverem.";
    }
}

[Trait("Category", "Integration")]
public sealed partial class SqlApiTests : IAsyncLifetime
{
    private readonly string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    private readonly string connection = new SqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("SZWALNIA_TEST_SQL"))
    {
        InitialCatalog = "SzwalniaTests_" + Guid.NewGuid().ToString("N"),
        Pooling = false
    }.ConnectionString;

    private WebApplicationFactory<Program>? factory;

    public Task InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = connection,
                    ["Seed:AdminPassword"] = password,
                    ["Seed:WorkerPassword"] = password,
                    ["Seed:SewingPassword"] = password,
                    ["Seed:Demo"] = "true"
                })));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        await using var db = new AppDb(new DbContextOptionsBuilder<AppDb>().UseSqlServer(connection).Options);
        await db.Database.EnsureDeletedAsync();
    }

    private HttpClient Client()
    {
        var client = factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Requested-With", "Szwalnia");
        return client;
    }

    private async Task Login(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { name, password });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> Get(HttpClient client, string path, CancellationToken ct = default)
    {
        using var response = await client.GetAsync(path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }

    private static async Task<int> Create(HttpClient client, string path, object data)
    {
        using var response = await client.PostAsJsonAsync(path, data);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task SetStatus(HttpClient client, string path, string status, int? actualQuantity = null, decimal? actualMeters = null)
    {
        string version = Version(await Get(client, path));
        object body = actualQuantity.HasValue ? new { status, version, actualQuantity, actualMeters } : new { status, version };
        using var response = await client.PostAsJsonAsync(path + "/status", body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static int FirstId(JsonElement document, string array) => document.GetProperty(array)[0].GetProperty("id").GetInt32();
    private static string Version(JsonElement document) => document.GetProperty("version").GetString()!;
    private static DateOnly WarsawDate() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Europe/Warsaw"));
}
