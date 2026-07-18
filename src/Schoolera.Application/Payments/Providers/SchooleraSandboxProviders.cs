using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Providers;

/// <summary>
/// Internal Development sandbox payment provider. Never performs real financial transactions.
/// </summary>
public sealed class SchooleraSandboxPaymentProvider : IPaymentProvider
{
    public const int MaxTimestampSkewSeconds = 300;

    public string ProviderCode => IntegrationProviderCodes.SchooleraSandbox;

    public Task<PaymentCheckoutSessionResult> CreateHostedCheckoutAsync(
        PaymentIntent intent,
        PaymentIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.Environment != ProviderEnvironment.Sandbox)
        {
            return Task.FromResult(new PaymentCheckoutSessionResult(
                false, null, null, "payments.sandboxEnvironmentRequired", false));
        }

        var sessionId = $"sbx_chk_{Guid.NewGuid():N}";
        var redirectUrl = $"/parent/payments/return?ref={Uri.EscapeDataString(intent.Reference)}";
        return Task.FromResult(new PaymentCheckoutSessionResult(true, sessionId, redirectUrl, null, false));
    }

    public Task<PaymentProviderStatusResult> QueryStatusAsync(
        PaymentIntent intent,
        PaymentIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.Environment != ProviderEnvironment.Sandbox)
        {
            return Task.FromResult(new PaymentProviderStatusResult(
                false, null, null, null, null, "payments.providerNotConfigured"));
        }

        var parsed = IntegrationSettingsSerializer.Deserialize<PaymentIntegrationSettings>(settings.SettingsJson);
        var outcome = NormalizeOutcome(parsed.SandboxDefaultOutcome);

        return Task.FromResult(outcome switch
        {
            "pending" => new PaymentProviderStatusResult(
                true, PaymentIntentStatus.Processing, intent.ProviderPaymentReference,
                intent.Amount, intent.CurrencyCode, null),
            "failed" => new PaymentProviderStatusResult(
                true, PaymentIntentStatus.Failed, null, intent.Amount, intent.CurrencyCode,
                "payments.sandboxFailed"),
            "cancel" => new PaymentProviderStatusResult(
                true, PaymentIntentStatus.Cancelled, null, intent.Amount, intent.CurrencyCode, null),
            "expire" => new PaymentProviderStatusResult(
                true, PaymentIntentStatus.Expired, null, intent.Amount, intent.CurrencyCode, null),
            _ => new PaymentProviderStatusResult(
                true, PaymentIntentStatus.Succeeded,
                intent.ProviderPaymentReference ?? $"sbx_pay_{intent.Id:N}",
                intent.Amount, intent.CurrencyCode, null),
        });
    }

    public Task<PaymentRefundResult> RefundAsync(
        PaymentIntent intent,
        decimal amount,
        string idempotencyKey,
        PaymentIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.Environment != ProviderEnvironment.Sandbox)
        {
            return Task.FromResult(new PaymentRefundResult(
                false, null, amount, "payments.refundNotSupported", false));
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)))[..8]
            .ToLowerInvariant();
        return Task.FromResult(new PaymentRefundResult(true, $"sbx_rfnd_{hash}", amount, null, false));
    }

    public bool VerifyWebhookSignature(
        string rawBody,
        string? signatureHex,
        string? timestampUnix,
        string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(signatureHex) ||
            string.IsNullOrWhiteSpace(timestampUnix) ||
            string.IsNullOrWhiteSpace(webhookSecret))
        {
            return false;
        }

        if (!long.TryParse(timestampUnix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - unix) > MaxTimestampSkewSeconds)
        {
            return false;
        }

        var payload = $"{timestampUnix}.{rawBody}";
        var expected = ComputeHmacHex(webhookSecret, payload);

        byte[] expectedBytes;
        byte[] actualBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expected);
            actualBytes = Convert.FromHexString(signatureHex.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    public static string ComputeHmacHex(string secret, string payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string NormalizeOutcome(string? outcome)
    {
        var value = (outcome ?? "success").Trim().ToLowerInvariant();
        return value is "pending" or "failed" or "cancel" or "expire" or "success" ? value : "success";
    }

    public static SandboxPaymentWebhookPayload? TryParsePayload(string rawBody)
    {
        try
        {
            return JsonSerializer.Deserialize<SandboxPaymentWebhookPayload>(
                rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}

public sealed class SandboxPaymentWebhookPayload
{
    public string? EventId { get; set; }

    public string? EventType { get; set; }

    public string? CheckoutSessionId { get; set; }

    public string? PaymentReference { get; set; }

    public string? IntentReference { get; set; }

    public decimal? Amount { get; set; }

    public string? CurrencyCode { get; set; }

    public string? Status { get; set; }
}

/// <summary>Internal Development sandbox financing provider — no real credit decisions.</summary>
public sealed class SchooleraSandboxFinancingProvider : IFinancingProvider
{
    public string ProviderCode => IntegrationProviderCodes.SchooleraSandbox;

    public Task<FinancingOffersResult> RequestOffersAsync(
        FinancingRequest request,
        FinancingIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.Environment != ProviderEnvironment.Sandbox)
        {
            return Task.FromResult(new FinancingOffersResult(
                false, null, [], "payments.providerNotConfigured"));
        }

        var parsed = IntegrationSettingsSerializer.Deserialize<FinancingIntegrationSettings>(settings.SettingsJson);
        var outcome = (parsed.SandboxDefaultOutcome ?? "offers").Trim().ToLowerInvariant();
        var providerRef = $"sbx_fin_{Guid.NewGuid():N}";

        if (outcome is "decline")
        {
            return Task.FromResult(new FinancingOffersResult(true, providerRef, [], null));
        }

        if (outcome is "expire")
        {
            return Task.FromResult(new FinancingOffersResult(
                true,
                providerRef,
                [
                    new FinancingOfferDraft(
                        $"sbx_off_{Guid.NewGuid():N}",
                        6,
                        Math.Round(request.Amount / 6m, 2, MidpointRounding.AwayFromZero),
                        request.Amount,
                        0m,
                        null,
                        null,
                        null,
                        DateTimeOffset.UtcNow.AddMinutes(-1),
                        "Sandbox expired offer — not a real financing offer."),
                ],
                null));
        }

        var tenors = parsed.SupportedTenorsMonths is { Length: > 0 }
            ? parsed.SupportedTenorsMonths
            : [3, 6, 12];

        var offers = tenors.Select(tenor =>
        {
            var fee = Math.Round(request.Amount * 0.02m, 2, MidpointRounding.AwayFromZero);
            var total = request.Amount + fee;
            var installment = Math.Round(total / tenor, 2, MidpointRounding.AwayFromZero);
            decimal? apr = parsed.DisplayProviderFees ? 12.5m : null;
            return new FinancingOfferDraft(
                $"sbx_off_{Guid.NewGuid():N}",
                tenor,
                installment,
                total,
                fee,
                null,
                apr,
                null,
                DateTimeOffset.UtcNow.AddHours(24),
                "Sandbox informational offer. Not a real credit approval. Provider-reported values only.");
        }).ToList();

        return Task.FromResult(new FinancingOffersResult(true, providerRef, offers, null));
    }

    public Task<FinancingProviderStatusResult> QueryStatusAsync(
        FinancingRequest request,
        FinancingIntegrationSettingsContext settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.Environment != ProviderEnvironment.Sandbox)
        {
            return Task.FromResult(new FinancingProviderStatusResult(
                false, null, "payments.providerNotConfigured"));
        }

        var status = request.Status switch
        {
            FinancingRequestStatus.OfferSelected => FinancingRequestStatus.ProviderReview,
            FinancingRequestStatus.ProviderReview => FinancingRequestStatus.Approved,
            FinancingRequestStatus.Approved => FinancingRequestStatus.FundingPending,
            FinancingRequestStatus.FundingPending => FinancingRequestStatus.Funded,
            _ => request.Status,
        };

        return Task.FromResult(new FinancingProviderStatusResult(true, status, null));
    }

    public bool VerifyWebhookSignature(
        string rawBody,
        string? signatureHex,
        string? timestampUnix,
        string webhookSecret) =>
        new SchooleraSandboxPaymentProvider()
            .VerifyWebhookSignature(rawBody, signatureHex, timestampUnix, webhookSecret);
}

public sealed class SandboxFinancingWebhookPayload
{
    public string? EventId { get; set; }

    public string? EventType { get; set; }

    public string? RequestReference { get; set; }

    public string? Status { get; set; }

    public static SandboxFinancingWebhookPayload? TryParse(string rawBody)
    {
        try
        {
            return JsonSerializer.Deserialize<SandboxFinancingWebhookPayload>(
                rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}
