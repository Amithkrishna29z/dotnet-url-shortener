namespace SnipLink.Application.Links;

public interface ILinkService
{
    Task<CreateLinkResponse> CreateAsync(CreateLinkRequest request, string? ownerToken = null, CancellationToken ct = default);

    Task<RedirectResolution> ResolveForRedirectAsync(string code, CancellationToken ct = default);

    Task<LinkResponse> GetAsync(string code, string ownerToken, CancellationToken ct = default);

    Task<StatsResponse> GetStatsAsync(string code, string ownerToken, CancellationToken ct = default);

    Task<LinkResponse> UpdateAsync(string code, string ownerToken, UpdateLinkRequest request, CancellationToken ct = default);

    Task DeactivateAsync(string code, string ownerToken, CancellationToken ct = default);

    Task<IReadOnlyList<LinkResponse>> GetByOwnerAsync(string ownerToken, CancellationToken ct = default);
}
