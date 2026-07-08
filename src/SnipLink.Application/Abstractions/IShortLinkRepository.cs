using SnipLink.Domain.Entities;

namespace SnipLink.Application.Abstractions;

public interface IShortLinkRepository
{
    Task<ShortLink?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);

    Task AddAsync(ShortLink link, CancellationToken ct = default);

    Task UpdateAsync(ShortLink link, CancellationToken ct = default);

    Task<IReadOnlyList<ShortLink>> GetByOwnerTokenAsync(string ownerToken, CancellationToken ct = default);
}
