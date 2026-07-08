using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SnipLink.Infrastructure.Persistence;

namespace SnipLink.IntegrationTests;

/// <summary>
/// Boots the real web app but swaps PostgreSQL for an in-memory EF provider and
/// disables Redis (empty connection string → the app's NullLinkCache). This exercises
/// the full pipeline — routing, validation, redirect, click recording — without
/// external infrastructure.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "sniplink-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // A dummy Postgres string satisfies AddInfrastructure; the DbContext is
                // replaced with the in-memory provider below so it's never used.
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=ignored;Username=x;Password=x",
                ["ConnectionStrings:Redis"] = "",
                ["SnipLink:BaseUrl"] = "http://localhost",
                ["SnipLink:IpHashSalt"] = "test-salt"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}
