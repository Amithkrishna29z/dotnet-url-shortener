namespace SnipLink.Application.Links;

/// <summary>Request to create a short link. Alias and expiry are optional.</summary>
public record CreateLinkRequest(string LongUrl, string? Alias, DateTime? ExpiresAt);

/// <summary>Result of creating a link. <see cref="OwnerToken"/> is the secret to manage it.</summary>
public record CreateLinkResponse(
    string Code,
    string ShortUrl,
    string LongUrl,
    string OwnerToken,
    DateTime? ExpiresAt);

/// <summary>Metadata about a link (no owner token echoed back).</summary>
public record LinkResponse(
    string Code,
    string ShortUrl,
    string LongUrl,
    DateTime? ExpiresAt,
    bool IsActive,
    DateTime CreatedAt,
    long TotalClicks);

/// <summary>Patch request: change expiry and/or active flag. Null fields are left unchanged.</summary>
public record UpdateLinkRequest(DateTime? ExpiresAt, bool? IsActive);

public record DailyPoint(DateOnly Date, int Count);

public record ReferrerCount(string Referrer, long Count);

/// <summary>Analytics for a link: total clicks, a daily series, and top referrers.</summary>
public record StatsResponse(
    string Code,
    long TotalClicks,
    IReadOnlyList<DailyPoint> Daily,
    IReadOnlyList<ReferrerCount> TopReferrers);

/// <summary>Per-visit data captured at redirect time. IP is already hashed.</summary>
public record ClickInfo(
    Guid ShortLinkId,
    DateTime ClickedAt,
    string? Referrer,
    string? UserAgent,
    string? IpHash,
    string? Country);
