namespace SnipLink.Application;

public class LinkNotFoundException(string code)
    : Exception($"No link found for code '{code}'.")
{
    public string Code { get; } = code;
}

public class OwnerTokenMismatchException()
    : Exception("The supplied owner token does not match this link.");

public class AliasConflictException(string alias)
    : Exception($"The alias '{alias}' is already in use.")
{
    public string Alias { get; } = alias;
}
