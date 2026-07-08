using System.Security.Cryptography;

namespace SnipLink.Domain;

/// <summary>
/// Generates random base62 (0-9, a-z, A-Z) strings for short codes.
/// Uses a cryptographically secure RNG with rejection sampling so the
/// distribution across the 62 characters stays uniform (no modulo bias).
/// Collision handling against existing codes is the caller's responsibility.
/// </summary>
public static class Base62
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const int DefaultLength = 6;

    public static string Generate(int length = DefaultLength)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

        var chars = new char[length];
        // Largest multiple of 62 that fits in a byte; values at or above it are
        // rejected to keep the mapping unbiased.
        const int limit = byte.MaxValue - (byte.MaxValue % 62); // 248
        var buffer = new byte[1];
        var i = 0;

        while (i < length)
        {
            RandomNumberGenerator.Fill(buffer);
            if (buffer[0] >= limit)
                continue;
            chars[i++] = Alphabet[buffer[0] % 62];
        }

        return new string(chars);
    }
}
