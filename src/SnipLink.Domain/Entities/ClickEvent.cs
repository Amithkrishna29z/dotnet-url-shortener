namespace SnipLink.Domain.Entities;

public class ClickEvent
{
    public long Id { get; set; }

    public Guid ShortLinkId { get; set; }

    public DateTime ClickedAt { get; set; }

    public string? Referrer { get; set; }

    public string? UserAgent { get; set; }

    public string? IpHash { get; set; }

    public string? Country { get; set; }

    public ShortLink? ShortLink { get; set; }
}
