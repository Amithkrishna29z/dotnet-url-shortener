using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;

namespace SnipLink.Web.Infrastructure;

public static class RedirectEndpoint
{
    /// <summary>
    /// Maps the redirect hot path: GET /{code}. Resolves cache-first (DB fallback),
    /// enqueues the click without blocking, and redirects. 404 if unknown, 410 if gone.
    /// </summary>
    public static void MapRedirect(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{code}", async (
            string code,
            HttpContext http,
            ILinkService links,
            IClickRecorder clicks,
            IIpHasher ipHasher,
            IClock clock,
            CancellationToken ct) =>
        {
            var resolution = await links.ResolveForRedirectAsync(code, ct);

            switch (resolution.Status)
            {
                case RedirectStatus.NotFound:
                    return Results.NotFound();
                case RedirectStatus.Gone:
                    return Results.StatusCode(StatusCodes.Status410Gone);
            }

            // Fire-and-forget: enqueue the click; never block the redirect on it.
            clicks.Enqueue(new ClickInfo(
                resolution.ShortLinkId!.Value,
                clock.UtcNow,
                http.Referrer(),
                http.UserAgent(),
                ipHasher.Hash(http.ClientIp()),
                Country: null));

            // 302 so visits keep hitting us (301 would be cached by browsers and skip analytics).
            return Results.Redirect(resolution.LongUrl!, permanent: false);
        })
        .ExcludeFromDescription(); // keep it out of Swagger; it's the public redirect, not API
    }
}
