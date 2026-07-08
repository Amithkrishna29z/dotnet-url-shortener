using Microsoft.EntityFrameworkCore;
using SnipLink.Application.Abstractions;
using SnipLink.Domain.Entities;

namespace SnipLink.Infrastructure.Persistence;

public class ShortLinkRepository : IShortLinkRepository
{
    private readonly AppDbContext _db;

    public ShortLinkRepository(AppDbContext db) => _db = db;

    public Task<ShortLink?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.ShortLinks.FirstOrDefaultAsync(x => x.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        _db.ShortLinks.AnyAsync(x => x.Code == code, ct);

    public async Task AddAsync(ShortLink link, CancellationToken ct = default)
    {
        _db.ShortLinks.Add(link);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ShortLink link, CancellationToken ct = default)
    {
        _db.ShortLinks.Update(link);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ShortLink>> GetByOwnerTokenAsync(string ownerToken, CancellationToken ct = default) =>
        await _db.ShortLinks
            .Where(x => x.OwnerToken == ownerToken)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
}
