namespace SnipLink.Web.Infrastructure;

/// <summary>Helpers for pulling request metadata used by click recording and rate limiting.</summary>
public static class RequestInfo
{
    /// <summary>The client IP as a string, honouring forwarded headers (set up in Program).</summary>
    public static string? ClientIp(this HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    public static string? Referrer(this HttpContext context)
    {
        var referer = context.Request.Headers.Referer.ToString();
        return string.IsNullOrWhiteSpace(referer) ? null : Truncate(referer, 2048);
    }

    public static string? UserAgent(this HttpContext context)
    {
        var ua = context.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : Truncate(ua, 512);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
