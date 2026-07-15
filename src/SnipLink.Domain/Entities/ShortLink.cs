namespace SnipLink.Domain.Entities;

public class ShortLink
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string LongUrl { get; set; } = string.Empty;

    public string OwnerToken { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<ClickEvent> Clicks { get; set; } = new List<ClickEvent>();

    public ICollection<DailyStat> DailyStats { get; set; } = new List<DailyStat>();

    public bool IsFollowable(DateTime utcNow) =>
        IsActive && (ExpiresAt is null || ExpiresAt > utcNow);
}
