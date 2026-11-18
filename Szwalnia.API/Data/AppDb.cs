using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Szwalnia.API.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Roll> Rolls => Set<Roll>();
    public DbSet<Seamstress> Seamstresses => Set<Seamstress>();
    public DbSet<CutOrder> Cuts => Set<CutOrder>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<SewOrder> Sews => Set<SewOrder>();
    public DbSet<StatusChange> StatusChanges => Set<StatusChange>();
    public DbSet<ReportJob> Reports => Set<ReportJob>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasIndex(x => x.Name).IsUnique();
        model.Entity<Material>().HasIndex(x => x.Name).IsUnique();
        model.Entity<Batch>().HasIndex(x => x.CutOrderId).IsUnique();
        model.Entity<CutOrder>().HasIndex(x => new { x.Status, x.DueDate });
        model.Entity<SewOrder>().HasIndex(x => new { x.Status, x.DueDate });
        model.Entity<ReportJob>().HasIndex(x => new { x.Status, x.CreatedAt });
        foreach (var type in model.Model.GetEntityTypes())
        {
            foreach (var property in type.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?)) { property.SetPrecision(18); property.SetScale(4); }
                if (property.ClrType == typeof(string)) property.SetMaxLength(property.Name == "ResultJson" ? 100000 : property.Name == "PasswordHash" ? 1000 : 200);
            }
            foreach (var fk in type.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
        model.Entity<Roll>().ToTable(t => t.HasCheckConstraint("CK_Roll_Balance", "[RemainingMeters] >= 0 AND [ReservedMeters] >= 0 AND [ReservedMeters] <= [RemainingMeters]"));
        model.Entity<Batch>().ToTable(t => t.HasCheckConstraint("CK_Batch_Balance", "[Quantity] >= 0 AND [AllocatedQuantity] >= 0 AND [AllocatedQuantity] <= [Quantity]"));
    }
}

public class DesignTimeDbFactory : IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AppDb>().UseSqlServer(
        Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "Server=localhost;Database=SzwalniaFlow;Integrated Security=true;TrustServerCertificate=true").Options);
}