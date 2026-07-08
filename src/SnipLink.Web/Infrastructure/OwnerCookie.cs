using SnipLink.Domain;

namespace SnipLink.Web.Infrastructure;

/// <summary>
/// Manages the per-browser owner token cookie. Links created via the UI share this
/// token so the "My Links" page can list them. This is a lightweight stand-in for
/// real accounts (see README "future work").
/// </summary>
public static class OwnerCookie
{
    public const string Name = "sniplink_owner";

    public static string? Get(HttpContext context) =>
        context.Request.Cookies.TryGetValue(Name, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    public static string GetOrCreate(HttpContext context)
    {
        var existing = Get(context);
        if (existing is not null)
            return existing;

        var token = Base62.Generate(32);
        context.Response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });
        return token;
    }
}
