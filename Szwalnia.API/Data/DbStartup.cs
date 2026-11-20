using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Szwalnia.API.Data;

public static class DbStartup
{
    public static async Task Initialize(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
        await db.Database.MigrateAsync();

        await SeedUser("admin", UserRole.Admin, configuration["Seed:AdminPassword"], db, hasher);
        await SeedUser("worker", UserRole.Cutting, configuration["Seed:WorkerPassword"], db, hasher);
        await SeedUser("sewing", UserRole.Sewing, configuration["Seed:SewingPassword"], db, hasher);
        if (!configuration.GetValue<bool>("Seed:Demo") || await db.Materials.AnyAsync())
            return;

        var material = new Material { Name = "Bawełna demonstracyjna" };
        db.AddRange(
            new Product
            {
                Name = "Fartuch", Variant = "M", Material = material, WidthCm = 160, MetersPerSet = 1.2m,
                CutSetupMinutes = 10, CutMinutesPerSet = 3, CutSetupCost = 12, CutCostPerSet = 4,
                SewMinutesPerSet = 15, SewCostPerSet = 18
            },
            new Roll { Name = "BELKA-001", Material = material, WidthCm = 160, RemainingMeters = 100 },
            new Seamstress { Name = "Pracownia demonstracyjna" });
        await db.SaveChangesAsync();
    }

    private static async Task SeedUser(
        string name, string role, string? password, AppDb db, PasswordHasher<User> hasher)
    {
        if (string.IsNullOrEmpty(password) || await db.Users.AnyAsync(x => x.Name == name))
            return;
        if (password.Length is < 12 or > 200)
            throw new InvalidOperationException($"Hasło startowe konta {name} musi mieć od 12 do 200 znaków.");

        var user = new User { Name = name, Role = role };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
