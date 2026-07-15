using SnipLink.Application.Abstractions;
using SnipLink.Application.Links;

namespace SnipLink.Web.Infrastructure;

public static class RedirectEndpoint
{
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

            clicks.Enqueue(new ClickInfo(
                resolution.ShortLinkId!.Value,
                clock.UtcNow,
                http.Referrer(),
                http.UserAgent(),
                ipHasher.Hash(http.ClientIp()),
                Country: null));

            return Results.Redirect(resolution.LongUrl!, permanent: false);
        })
        .ExcludeFromDescription();
    }
}
