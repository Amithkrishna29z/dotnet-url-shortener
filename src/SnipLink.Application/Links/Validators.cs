using FluentValidation;

namespace SnipLink.Application.Links;

public class CreateLinkRequestValidator : AbstractValidator<CreateLinkRequest>
{
    public CreateLinkRequestValidator()
    {
        RuleFor(x => x.LongUrl)
            .NotEmpty().WithMessage("A URL is required.")
            .MaximumLength(2048)
            .Must(BeAValidAbsoluteHttpUrl)
            .WithMessage("Must be a valid absolute http(s) URL.");

        When(x => !string.IsNullOrEmpty(x.Alias), () =>
        {
            RuleFor(x => x.Alias!)
                .Length(AliasRules.MinLength, AliasRules.MaxLength)
                .WithMessage($"Alias must be {AliasRules.MinLength}–{AliasRules.MaxLength} characters.")
                .Must(AliasRules.IsAlphanumeric)
                .WithMessage("Alias must be alphanumeric.")
                .Must(a => !AliasRules.IsReserved(a))
                .WithMessage("That alias is reserved.");
        });

        RuleFor(x => x.ExpiresAt)
            .Must(e => e!.Value.ToUniversalTime() > DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiry must be in the future.");
    }

    private static bool BeAValidAbsoluteHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public class UpdateLinkRequestValidator : AbstractValidator<UpdateLinkRequest>
{
    public UpdateLinkRequestValidator()
    {
        RuleFor(x => x.ExpiresAt)
            .Must(e => e!.Value.ToUniversalTime() > DateTime.UtcNow)
            .When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiry must be in the future.");
    }
}
