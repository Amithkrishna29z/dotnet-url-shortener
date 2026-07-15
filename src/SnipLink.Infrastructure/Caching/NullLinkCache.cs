using SnipLink.Application.Abstractions;

namespace SnipLink.Infrastructure.Caching;

public class NullLinkCache : ILinkCache
{
    public Task<CachedLink?> GetAsync(string code, CancellationToken ct = default) =>
        Task.FromResult<CachedLink?>(null);

    public Task SetAsync(string code, CachedLink link, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task RemoveAsync(string code, CancellationToken ct = default) =>
        Task.CompletedTask;
}
