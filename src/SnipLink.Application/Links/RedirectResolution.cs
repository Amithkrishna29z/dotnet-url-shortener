namespace SnipLink.Application.Links;

public enum RedirectStatus
{
    Found,
    NotFound,
    Gone
}

public record RedirectResolution(RedirectStatus Status, string? LongUrl, Guid? ShortLinkId)
{
    public static RedirectResolution Found(string longUrl, Guid id) =>
        new(RedirectStatus.Found, longUrl, id);

    public static readonly RedirectResolution NotFound = new(RedirectStatus.NotFound, null, null);
    public static readonly RedirectResolution Gone = new(RedirectStatus.Gone, null, null);
}
