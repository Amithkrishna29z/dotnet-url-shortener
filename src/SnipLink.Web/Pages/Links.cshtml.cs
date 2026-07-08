using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SnipLink.Application.Links;
using SnipLink.Web.Infrastructure;

namespace SnipLink.Web.Pages;

public class LinksModel : PageModel
{
    private readonly ILinkService _links;

    public LinksModel(ILinkService links) => _links = links;

    public IReadOnlyList<LinkResponse> Links { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var ownerToken = OwnerCookie.Get(HttpContext);
        if (ownerToken is not null)
            Links = await _links.GetByOwnerAsync(ownerToken, ct);
    }

    public async Task<IActionResult> OnPostDeactivateAsync(string code, CancellationToken ct)
    {
        var ownerToken = OwnerCookie.Get(HttpContext);
        if (ownerToken is not null)
            await _links.DeactivateAsync(code, ownerToken, ct);
        return RedirectToPage();
    }
}
