using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SnipLink.Application;
using SnipLink.Application.Abstractions;
using StackExchange.Redis;

namespace SnipLink.Infrastructure.Caching;

public class RedisLinkCache : ILinkCache
{
    private const string KeyPrefix = "sniplink:code:";

    private readonly IConnectionMultiplexer _redis;
    private readonly SnipLinkOptions _options;
    private readonly ILogger<RedisLinkCache> _logger;

    public RedisLinkCache(
        IConnectionMultiplexer redis,
        IOptions<SnipLinkOptions> options,
        ILogger<RedisLinkCache> logger)
    {
        _redis = redis;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CachedLink?> GetAsync(string code, CancellationToken ct = default)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(Key(code));
            if (value.IsNullOrEmpty)
                return null;
            return JsonSerializer.Deserialize<CachedLink>(value!);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis GET failed for {Code}; falling back to database.", code);
            return null;
        }
    }

    public async Task SetAsync(string code, CachedLink link, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(link);
            await _redis.GetDatabase().StringSetAsync(Key(code), payload, _options.CacheTtl);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis SET failed for {Code}; continuing without cache.", code);
        }
    }

    public async Task RemoveAsync(string code, CancellationToken ct = default)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(Key(code));
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis DEL failed for {Code}.", code);
        }
    }

    private static RedisKey Key(string code) => KeyPrefix + code;

    private static bool IsRedisFailure(Exception ex) =>
        ex is RedisException or RedisTimeoutException or ObjectDisposedException;
}
