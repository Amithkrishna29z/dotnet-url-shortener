namespace SnipLink.Domain.Entities;

public class DailyStat
{
    public long Id { get; set; }

    public Guid ShortLinkId { get; set; }

    public DateOnly Date { get; set; }

    public int ClickCount { get; set; }

    public ShortLink? ShortLink { get; set; }
}
