using SnipLink.Domain;

namespace SnipLink.Web.Infrastructure;

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
