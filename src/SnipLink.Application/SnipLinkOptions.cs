namespace SnipLink.Application;

/// <summary>App-wide options bound from the "SnipLink" config section.</summary>
public class SnipLinkOptions
{
    public const string SectionName = "SnipLink";

    /// <summary>Base URL used to build short URLs, e.g. "http://localhost:8080".</summary>
    public string BaseUrl { get; set; } = "http://localhost:8080";

    /// <summary>Length of generated short codes.</summary>
    public int CodeLength { get; set; } = 6;

    /// <summary>How long a resolved code → URL stays in the cache.</summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Salt mixed into IP hashes. MUST be overridden in production config.</summary>
    public string IpHashSalt { get; set; } = "change-me";

    /// <summary>Number of top referrers returned by the stats endpoint.</summary>
    public int TopReferrersLimit { get; set; } = 10;
}
