using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SnipLink.Application;
using SnipLink.Application.Links;
using SnipLink.Web.Infrastructure;

namespace SnipLink.Web.Pages;

public class StatsModel : PageModel
{
    private readonly ILinkService _links;

    public StatsModel(ILinkService links) => _links = links;

    public StatsResponse? Stats { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string code, CancellationToken ct)
    {
        var ownerToken = OwnerCookie.Get(HttpContext);
        if (ownerToken is null)
        {
            Error = "No owner token found in this browser. Create a link first.";
            return Page();
        }

        try
        {
            Stats = await _links.GetStatsAsync(code, ownerToken, ct);
        }
        catch (LinkNotFoundException)
        {
            Error = "That link was not found.";
        }
        catch (OwnerTokenMismatchException)
        {
            Error = "This link was not created in this browser.";
        }

        return Page();
    }
}
