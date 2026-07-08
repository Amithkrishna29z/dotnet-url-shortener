using System.Text.RegularExpressions;

namespace SnipLink.Application.Links;

/// <summary>
/// Rules for custom aliases: 3–20 alphanumeric characters, not a reserved word
/// (reserved words collide with the app's own routes).
/// </summary>
public static partial class AliasRules
{
    public const int MinLength = 3;
    public const int MaxLength = 20;

    /// <summary>Route prefixes the redirect handler must never shadow.</summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "api", "swagger", "health", "healthz", "links", "stats",
        "admin", "css", "js", "lib", "favicon", "robots"
    };

    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex AlphanumericRegex();

    public static bool IsAlphanumeric(string alias) => AlphanumericRegex().IsMatch(alias);

    public static bool IsReserved(string alias) => Reserved.Contains(alias);
}
