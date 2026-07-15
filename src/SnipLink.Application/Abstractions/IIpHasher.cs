namespace SnipLink.Application.Abstractions;

public interface IIpHasher
{
    string? Hash(string? ip);
}
