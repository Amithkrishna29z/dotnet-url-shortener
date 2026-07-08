namespace SnipLink.Application.Abstractions;

/// <summary>
/// Hashes a visitor IP so the raw address is never stored or logged.
/// Returns null for a null/empty input.
/// </summary>
public interface IIpHasher
{
    string? Hash(string? ip);
}
