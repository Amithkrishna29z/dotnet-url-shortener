using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SnipLink.Infrastructure.Persistence;

/// <summary>
/// Used by the EF Core CLI (dotnet ef) at design time to build the context
/// without booting the full web host. The connection string here is only used
/// to scaffold migrations; runtime uses the configured connection string.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SNIPLINK_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=sniplink;Username=sniplink;Password=sniplink";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
