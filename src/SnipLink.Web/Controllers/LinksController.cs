using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SnipLink.Application.Links;

namespace SnipLink.Web.Controllers;

[ApiController]
[Route("api/v1/links")]
[Produces("application/json")]
public class LinksController : ControllerBase
{
    public const string OwnerTokenHeader = "X-Owner-Token";

    private readonly ILinkService _links;

    public LinksController(ILinkService links) => _links = links;

    /// <summary>Create a short link. Returns the code, short URL, and the owner token.</summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.CreateLink)]
    [ProducesResponseType(typeof(CreateLinkResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateLinkResponse>> Create(CreateLinkRequest request, CancellationToken ct)
    {
        var result = await _links.CreateAsync(request, ct: ct);
        return CreatedAtAction(nameof(Get), new { code = result.Code }, result);
    }

    /// <summary>Get link metadata. Requires the matching owner token.</summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(LinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LinkResponse>> Get(string code, CancellationToken ct)
    {
        var result = await _links.GetAsync(code, OwnerToken(), ct);
        return Ok(result);
    }

    /// <summary>Get analytics for a link: total clicks, daily series, top referrers.</summary>
    [HttpGet("{code}/stats")]
    [ProducesResponseType(typeof(StatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StatsResponse>> Stats(string code, CancellationToken ct)
    {
        var result = await _links.GetStatsAsync(code, OwnerToken(), ct);
        return Ok(result);
    }

    /// <summary>Update a link's expiry and/or active flag.</summary>
    [HttpPatch("{code}")]
    [ProducesResponseType(typeof(LinkResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LinkResponse>> Update(string code, UpdateLinkRequest request, CancellationToken ct)
    {
        var result = await _links.UpdateAsync(code, OwnerToken(), request, ct);
        return Ok(result);
    }

    /// <summary>Deactivate a link so it stops redirecting.</summary>
    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        await _links.DeactivateAsync(code, OwnerToken(), ct);
        return NoContent();
    }

    private string OwnerToken() =>
        Request.Headers.TryGetValue(OwnerTokenHeader, out var value) ? value.ToString() : string.Empty;
}
