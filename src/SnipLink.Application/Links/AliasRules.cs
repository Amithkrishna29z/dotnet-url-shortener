using System.Text.RegularExpressions;

namespace SnipLink.Application.Links;

public static partial class AliasRules
{
    public const int MinLength = 3;
    public const int MaxLength = 20;

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
