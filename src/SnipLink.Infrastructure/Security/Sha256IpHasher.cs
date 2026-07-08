using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SnipLink.Application;
using SnipLink.Application.Abstractions;

namespace SnipLink.Infrastructure.Security;

/// <summary>
/// Hashes IPs with SHA-256 over a configured salt so raw addresses are never stored.
/// The salt makes the stored hashes resistant to trivial rainbow-table reversal.
/// </summary>
public class Sha256IpHasher : IIpHasher
{
    private readonly string _salt;

    public Sha256IpHasher(IOptions<SnipLinkOptions> options) => _salt = options.Value.IpHashSalt;

    public string? Hash(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return null;

        var bytes = Encoding.UTF8.GetBytes(_salt + ip);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
