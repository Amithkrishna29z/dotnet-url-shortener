using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnipLink.Application.Abstractions;
using SnipLink.Infrastructure.Caching;
using SnipLink.Infrastructure.Clicks;
using SnipLink.Infrastructure.Persistence;
using SnipLink.Infrastructure.Security;
using StackExchange.Redis;

namespace SnipLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IShortLinkRepository, ShortLinkRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IClickAggregator, ClickAggregator>();

        services.AddSingleton<IIpHasher, Sha256IpHasher>();

        AddCache(services, configuration);

        // The recorder is a singleton holding the in-memory click buffer; the redirect
        // endpoint enqueues into it and ClickFlushService (registered by the web host) drains it.
        services.AddSingleton<ChannelClickRecorder>();
        services.AddSingleton<IClickRecorder>(sp => sp.GetRequiredService<ChannelClickRecorder>());

        return services;
    }

    private static void AddCache(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            // No Redis configured — redirects resolve straight from the database.
            services.AddSingleton<ILinkCache, NullLinkCache>();
            return;
        }

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            // Don't fail startup or throw mid-request if Redis is unreachable;
            // RedisLinkCache catches failures and falls back to the database.
            options.AbortOnConnectFail = false;
            var logger = sp.GetRequiredService<ILogger<RedisLinkCache>>();
            var muxer = ConnectionMultiplexer.Connect(options);
            muxer.ConnectionFailed += (_, e) =>
                logger.LogWarning("Redis connection failed: {FailureType}", e.FailureType);
            return muxer;
        });

        services.AddSingleton<ILinkCache, RedisLinkCache>();
    }
}
