namespace SnipLink.Application.Links;

public interface ILinkService
{
    /// <summary>
    /// Create a short link. If <paramref name="ownerToken"/> is supplied (e.g. the trusted
    /// UI reusing a per-browser token), it is used; otherwise a fresh token is generated.
    /// </summary>
    Task<CreateLinkResponse> CreateAsync(CreateLinkRequest request, string? ownerToken = null, CancellationToken ct = default);

    /// <summary>Resolve a code for redirect. Hot path: cache-first, DB fallback.</summary>
    Task<RedirectResolution> ResolveForRedirectAsync(string code, CancellationToken ct = default);

    Task<LinkResponse> GetAsync(string code, string ownerToken, CancellationToken ct = default);

    Task<StatsResponse> GetStatsAsync(string code, string ownerToken, CancellationToken ct = default);

    Task<LinkResponse> UpdateAsync(string code, string ownerToken, UpdateLinkRequest request, CancellationToken ct = default);

    /// <summary>Deactivate (soft-delete) a link so it stops redirecting.</summary>
    Task DeactivateAsync(string code, string ownerToken, CancellationToken ct = default);

    Task<IReadOnlyList<LinkResponse>> GetByOwnerAsync(string ownerToken, CancellationToken ct = default);
}
