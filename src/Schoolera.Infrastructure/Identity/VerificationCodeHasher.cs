using System.Security.Cryptography;
using System.Text;

namespace Schoolera.Infrastructure.Identity;

internal static class VerificationCodeHasher
{
    public static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string code, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            var computed = Convert.FromHexString(Hash(code));
            var stored = Convert.FromHexString(hash);
            if (computed.Length != stored.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(computed, stored);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
