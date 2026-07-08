namespace SnipLink.Domain.Entities;

/// <summary>
/// A single visit to a short link. Raw click events are the source of truth;
/// the worker rolls them up into <see cref="DailyStat"/> rows.
/// IPs are never stored raw — only a salted hash, for privacy.
/// </summary>
public class ClickEvent
{
    public long Id { get; set; }

    public Guid ShortLinkId { get; set; }

    public DateTime ClickedAt { get; set; }

    public string? Referrer { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>Salted hash of the visitor IP. Never the raw address.</summary>
    public string? IpHash { get; set; }

    public string? Country { get; set; }

    public ShortLink? ShortLink { get; set; }
}
