using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SnipLink.Application;
using SnipLink.Application.Links;
using SnipLink.Web.Infrastructure;

namespace SnipLink.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ILinkService _links;

    public IndexModel(ILinkService links) => _links = links;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public CreateLinkResponse? Created { get; private set; }

    public class InputModel
    {
        [Required]
        [Url(ErrorMessage = "Enter a valid absolute URL (http or https).")]
        [Display(Name = "Long URL")]
        public string LongUrl { get; set; } = string.Empty;

        [Display(Name = "Custom alias (optional)")]
        public string? Alias { get; set; }

        [Display(Name = "Expires at (optional, UTC)")]
        public DateTime? ExpiresAt { get; set; }
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        var ownerToken = OwnerCookie.GetOrCreate(HttpContext);
        var request = new CreateLinkRequest(Input.LongUrl, Input.Alias, Input.ExpiresAt);

        try
        {
            Created = await _links.CreateAsync(request, ownerToken, ct);
        }
        catch (AliasConflictException)
        {
            ModelState.AddModelError("Input.Alias", "That alias is already taken.");
            return Page();
        }

        ModelState.Clear();
        Input = new InputModel();
        return Page();
    }
}
