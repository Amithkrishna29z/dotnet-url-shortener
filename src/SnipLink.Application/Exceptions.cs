namespace SnipLink.Application;

/// <summary>Requested link does not exist. Maps to HTTP 404.</summary>
public class LinkNotFoundException(string code)
    : Exception($"No link found for code '{code}'.")
{
    public string Code { get; } = code;
}

/// <summary>Owner token did not match the stored token. Maps to HTTP 403.</summary>
public class OwnerTokenMismatchException()
    : Exception("The supplied owner token does not match this link.");

/// <summary>Requested alias is already taken. Maps to HTTP 409.</summary>
public class AliasConflictException(string alias)
    : Exception($"The alias '{alias}' is already in use.")
{
    public string Alias { get; } = alias;
}
