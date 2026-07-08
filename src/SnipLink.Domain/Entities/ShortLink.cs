namespace SnipLink.Domain.Entities;

/// <summary>
/// A shortened link. The <see cref="Code"/> is the public short identifier;
/// <see cref="OwnerToken"/> is a secret returned at creation so an anonymous
/// creator can manage the link without an account.
/// </summary>
public class ShortLink
{
    public Guid Id { get; set; }

    /// <summary>The short code, e.g. "aB3xY". Unique and indexed.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The destination URL. Validated as an absolute URL on creation.</summary>
    public string LongUrl { get; set; } = string.Empty;

    /// <summary>Secret token returned at creation; required to manage this link.</summary>
    public string OwnerToken { get; set; } = string.Empty;

    /// <summary>Optional expiry (UTC). A link past this time returns 410 Gone.</summary>
    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<ClickEvent> Clicks { get; set; } = new List<ClickEvent>();

    public ICollection<DailyStat> DailyStats { get; set; } = new List<DailyStat>();

    /// <summary>True when the link can still be followed (active and not expired).</summary>
    public bool IsFollowable(DateTime utcNow) =>
        IsActive && (ExpiresAt is null || ExpiresAt > utcNow);
}
