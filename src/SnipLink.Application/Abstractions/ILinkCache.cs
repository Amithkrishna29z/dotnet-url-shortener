namespace SnipLink.Application.Abstractions;

public record CachedLink(Guid Id, string LongUrl, DateTime? ExpiresAt);

public interface ILinkCache
{
    Task<CachedLink?> GetAsync(string code, CancellationToken ct = default);

    Task SetAsync(string code, CachedLink link, CancellationToken ct = default);

    Task RemoveAsync(string code, CancellationToken ct = default);
}
