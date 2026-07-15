using System.Security.Cryptography;

namespace SnipLink.Domain;

public static class Base62
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const int DefaultLength = 6;

    public static string Generate(int length = DefaultLength)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

        var chars = new char[length];
        const int limit = byte.MaxValue - (byte.MaxValue % 62);
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
