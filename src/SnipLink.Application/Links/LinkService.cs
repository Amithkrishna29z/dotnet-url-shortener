using Microsoft.Extensions.Options;
using SnipLink.Application.Abstractions;
using SnipLink.Domain;
using SnipLink.Domain.Entities;

namespace SnipLink.Application.Links;

public class LinkService : ILinkService
{
    private const int MaxCodeGenerationAttempts = 5;
    private const int OwnerTokenLength = 32;

    private readonly IShortLinkRepository _links;
    private readonly IAnalyticsRepository _analytics;
    private readonly ILinkCache _cache;
    private readonly IClock _clock;
    private readonly SnipLinkOptions _options;

    public LinkService(
        IShortLinkRepository links,
        IAnalyticsRepository analytics,
        ILinkCache cache,
        IClock clock,
        IOptions<SnipLinkOptions> options)
    {
        _links = links;
        _analytics = analytics;
        _cache = cache;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<CreateLinkResponse> CreateAsync(CreateLinkRequest request, string? ownerToken = null, CancellationToken ct = default)
    {
        var code = string.IsNullOrWhiteSpace(request.Alias)
            ? await GenerateUniqueCodeAsync(ct)
            : await ReserveAliasAsync(request.Alias!, ct);

        var link = new ShortLink
        {
            Id = Guid.NewGuid(),
            Code = code,
            LongUrl = request.LongUrl,
            OwnerToken = string.IsNullOrWhiteSpace(ownerToken) ? Base62.Generate(OwnerTokenLength) : ownerToken,
            ExpiresAt = request.ExpiresAt?.ToUniversalTime(),
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };

        await _links.AddAsync(link, ct);

        return new CreateLinkResponse(
            link.Code,
            BuildShortUrl(link.Code),
            link.LongUrl,
            link.OwnerToken,
            link.ExpiresAt);
    }

    public async Task<RedirectResolution> ResolveForRedirectAsync(string code, CancellationToken ct = default)
    {
        var cached = await _cache.GetAsync(code, ct);
        if (cached is not null)
        {
            if (IsExpired(cached.ExpiresAt))
            {
                await _cache.RemoveAsync(code, ct);
                return RedirectResolution.Gone;
            }
            return RedirectResolution.Found(cached.LongUrl, cached.Id);
        }

        var link = await _links.GetByCodeAsync(code, ct);
        if (link is null)
            return RedirectResolution.NotFound;

        if (!link.IsActive || IsExpired(link.ExpiresAt))
            return RedirectResolution.Gone;

        await _cache.SetAsync(code, new CachedLink(link.Id, link.LongUrl, link.ExpiresAt), ct);
        return RedirectResolution.Found(link.LongUrl, link.Id);
    }

    public async Task<LinkResponse> GetAsync(string code, string ownerToken, CancellationToken ct = default)
    {
        var link = await GetOwnedLinkAsync(code, ownerToken, ct);
        var total = await _analytics.GetTotalClicksAsync(link.Id, ct);
        return ToResponse(link, total);
    }

    public async Task<StatsResponse> GetStatsAsync(string code, string ownerToken, CancellationToken ct = default)
    {
        var link = await GetOwnedLinkAsync(code, ownerToken, ct);
        var total = await _analytics.GetTotalClicksAsync(link.Id, ct);
        var daily = await _analytics.GetDailySeriesAsync(link.Id, ct);
        var referrers = await _analytics.GetTopReferrersAsync(link.Id, _options.TopReferrersLimit, ct);
        return new StatsResponse(link.Code, total, daily, referrers);
    }

    public async Task<LinkResponse> UpdateAsync(string code, string ownerToken, UpdateLinkRequest request, CancellationToken ct = default)
    {
        var link = await GetOwnedLinkAsync(code, ownerToken, ct);

        if (request.ExpiresAt.HasValue)
            link.ExpiresAt = request.ExpiresAt.Value.ToUniversalTime();
        if (request.IsActive.HasValue)
            link.IsActive = request.IsActive.Value;

        await _links.UpdateAsync(link, ct);
        await _cache.RemoveAsync(code, ct);

        var total = await _analytics.GetTotalClicksAsync(link.Id, ct);
        return ToResponse(link, total);
    }

    public async Task DeactivateAsync(string code, string ownerToken, CancellationToken ct = default)
    {
        var link = await GetOwnedLinkAsync(code, ownerToken, ct);
        link.IsActive = false;
        await _links.UpdateAsync(link, ct);
        await _cache.RemoveAsync(code, ct);
    }

    public async Task<IReadOnlyList<LinkResponse>> GetByOwnerAsync(string ownerToken, CancellationToken ct = default)
    {
        var links = await _links.GetByOwnerTokenAsync(ownerToken, ct);
        var result = new List<LinkResponse>(links.Count);
        foreach (var link in links)
        {
            var total = await _analytics.GetTotalClicksAsync(link.Id, ct);
            result.Add(ToResponse(link, total));
        }
        return result;
    }

    private async Task<ShortLink> GetOwnedLinkAsync(string code, string ownerToken, CancellationToken ct)
    {
        var link = await _links.GetByCodeAsync(code, ct)
            ?? throw new LinkNotFoundException(code);

        if (!string.Equals(link.OwnerToken, ownerToken, StringComparison.Ordinal))
            throw new OwnerTokenMismatchException();

        return link;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            var code = Base62.Generate(_options.CodeLength);
            if (!await _links.CodeExistsAsync(code, ct))
                return code;
        }

        throw new InvalidOperationException(
            $"Could not generate a unique code after {MaxCodeGenerationAttempts} attempts.");
    }

    private async Task<string> ReserveAliasAsync(string alias, CancellationToken ct)
    {
        if (await _links.CodeExistsAsync(alias, ct))
            throw new AliasConflictException(alias);
        return alias;
    }

    private bool IsExpired(DateTime? expiresAt) => expiresAt is not null && expiresAt <= _clock.UtcNow;

    private string BuildShortUrl(string code) => $"{_options.BaseUrl.TrimEnd('/')}/{code}";

    private LinkResponse ToResponse(ShortLink link, long totalClicks) => new(
        link.Code,
        BuildShortUrl(link.Code),
        link.LongUrl,
        link.ExpiresAt,
        link.IsActive,
        link.CreatedAt,
        totalClicks);
}
