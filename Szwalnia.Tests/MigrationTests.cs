using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Szwalnia.API.Data;
using Xunit;

namespace Szwalnia.Tests;

public class MigrationTests
{
    [Fact]
    public void SqlServerSchemaHasAMigrationMatchingTheCurrentModel()
    {
        var options = new DbContextOptionsBuilder<AppDb>()
            .UseSqlServer("Server=localhost;Database=SzwalniaMigrationCheck;Integrated Security=True")
            .Options;
        using var db = new AppDb(options);

        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE [Cuts]", script);
        Assert.Contains("CREATE TABLE [Reports]", script);
    }
}
