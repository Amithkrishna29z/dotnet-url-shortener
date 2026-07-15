namespace SnipLink.Application.Links;

public record CreateLinkRequest(string LongUrl, string? Alias, DateTime? ExpiresAt);

public record CreateLinkResponse(
    string Code,
    string ShortUrl,
    string LongUrl,
    string OwnerToken,
    DateTime? ExpiresAt);

public record LinkResponse(
    string Code,
    string ShortUrl,
    string LongUrl,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt,
    long TotalClicks);

public record UpdateLinkRequest(DateTime? ExpiresAt, bool? IsActive);

public record DailyPoint(DateOnly Date, int Count);

public record ReferrerCount(string Referrer, long Count);

public record StatsResponse(
    string Code,
    long TotalClicks,
    IReadOnlyList<DailyPoint> Daily,
    IReadOnlyList<ReferrerCount> TopReferrers);

public record ClickInfo(
    Guid ShortLinkId,
    DateTime ClickedAt,
    string? Referrer,
    string? UserAgent,
    string? IpHash,
    string? Country);
