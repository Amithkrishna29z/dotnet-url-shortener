namespace SnipLink.Application.Abstractions;

/// <summary>Minimal link data cached for the redirect hot path.</summary>
public record CachedLink(Guid Id, string LongUrl, DateTime? ExpiresAt);

/// <summary>
/// Caches resolved links for the redirect hot path. Implementations MUST degrade
/// gracefully: if the cache backend is unavailable, methods should swallow the error
/// (returning null / doing nothing) so the redirect falls back to the database.
/// </summary>
public interface ILinkCache
{
    Task<CachedLink?> GetAsync(string code, CancellationToken ct = default);

    Task SetAsync(string code, CachedLink link, CancellationToken ct = default);

    Task RemoveAsync(string code, CancellationToken ct = default);
}
