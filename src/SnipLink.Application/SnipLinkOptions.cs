namespace SnipLink.Application;

public class SnipLinkOptions
{
    public const string SectionName = "SnipLink";

    public string BaseUrl { get; set; } = "http://localhost:8080";

    public int CodeLength { get; set; } = 6;

    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

    public string IpHashSalt { get; set; } = "change-me";

    public int TopReferrersLimit { get; set; } = 10;
}
