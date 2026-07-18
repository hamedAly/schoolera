using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Parent.Options;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Parent;

public sealed class ChildIdentityProtector(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<ParentIdentityProtectionOptions> options) : IChildIdentityProtector
{
    private const string ProtectorPurpose = "Schoolera.Parent.ChildIdentity.v1";
    private static readonly Regex NonDigit = new(@"\D+", RegexOptions.Compiled);

    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);

    public string Normalize(string identityValue)
    {
        var trimmed = identityValue.Trim();
        return NonDigit.Replace(trimmed, string.Empty);
    }

    public bool IsValidFormat(ChildIdentityType identityType, string normalizedValue)
    {
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return false;
        }

        // Generic digit-length rules only — no official national-ID checksum is documented for Schoolera.
        return identityType switch
        {
            ChildIdentityType.NationalId => normalizedValue.Length is >= 10 and <= 14,
            ChildIdentityType.ResidencyId => normalizedValue.Length is >= 8 and <= 16,
            _ => false,
        };
    }

    public string ComputeLookupHash(string normalizedValue)
    {
        var keyBytes = ResolveHmacKey();
        var payload = Encoding.UTF8.GetBytes(normalizedValue);
        var hash = HMACSHA256.HashData(keyBytes, payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public string Protect(string normalizedValue) =>
        _protector.Protect(normalizedValue);

    public string ExtractLastFour(string normalizedValue) =>
        normalizedValue.Length <= 4
            ? normalizedValue
            : normalizedValue[^4..];

    public string Mask(string identityLastFour) =>
        ChildIdentityMasking.MaskLastFour(identityLastFour);

    private byte[] ResolveHmacKey()
    {
        var configured = options.Value.HmacKeyBase64;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var bytes = Convert.FromBase64String(configured);
                if (bytes.Length >= 32)
                {
                    return bytes;
                }
            }
            catch (FormatException)
            {
                // Fall through to development ephemeral key derivation.
            }
        }

        // Development fallback — deterministic but not for Production key management.
        // Production must set ParentIdentityProtection:HmacKeyBase64 via secrets.
        return SHA256.HashData(Encoding.UTF8.GetBytes("Schoolera.Dev.ParentIdentity.Hmac.v1"));
    }
}
