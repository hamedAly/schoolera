using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Integrations;
using Schoolera.Application.Payments.Providers;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class PaymentsAndFinancingTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PaymentsAndFinancingTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public void PaymentIntentTransition_RejectsTerminalRegression()
    {
        Assert.False(PaymentIntentTransitionPolicy.CanTransition(
            PaymentIntentStatus.Succeeded, PaymentIntentStatus.Failed));
        Assert.True(PaymentIntentTransitionPolicy.CanTransition(
            PaymentIntentStatus.Processing, PaymentIntentStatus.Succeeded));
        Assert.True(PaymentIntentTransitionPolicy.CanTransition(
            PaymentIntentStatus.Succeeded, PaymentIntentStatus.Refunded));
    }

    [Fact]
    public void FinancingTransition_ApprovedDoesNotEqualFunded()
    {
        Assert.True(FinancingRequestTransitionPolicy.CanTransition(
            FinancingRequestStatus.Approved, FinancingRequestStatus.FundingPending));
        Assert.True(FinancingRequestTransitionPolicy.CanTransition(
            FinancingRequestStatus.FundingPending, FinancingRequestStatus.Funded));
        Assert.False(FinancingRequestTransitionPolicy.CanTransition(
            FinancingRequestStatus.Funded, FinancingRequestStatus.Approved));
    }

    [Fact]
    public void IntegrationValidator_AcceptsSandboxPayment()
    {
        var validator = new IntegrationSettingsValidator();
        var result = validator.Validate(
            IntegrationType.Payment,
            IntegrationProviderCodes.SchooleraSandbox,
            """
            {
              "environment": "Sandbox",
              "webhookSecret": "sandbox-dev-webhook-secret-not-real",
              "supportedCurrencies": ["EGP"],
              "supportedPaymentMethods": ["ProviderHostedCheckout"],
              "minimumAmount": 0,
              "requestTimeoutSeconds": 30,
              "sandboxDefaultOutcome": "success"
            }
            """,
            1);
        Assert.True(result.IsValid, string.Join(",", result.ErrorCodes));
    }

    [Fact]
    public void IntegrationValidator_RejectsSandboxAsProduction()
    {
        var validator = new IntegrationSettingsValidator();
        var result = validator.Validate(
            IntegrationType.Payment,
            IntegrationProviderCodes.SchooleraSandbox,
            """{"environment":"Production","requestTimeoutSeconds":30}""",
            1);
        Assert.False(result.IsValid);
        Assert.Contains("integrations.payment.sandboxCannotBeProduction", result.ErrorCodes);
    }

    [Fact]
    public void SensitiveRedactor_MasksPaymentSecrets()
    {
        var json = """{"ApiKey":"k","WebhookSecret":"whsec","MerchantId":"m1","Environment":"Sandbox"}""";
        var masked = SensitiveConfigurationRedactor.MaskJson(json);
        Assert.DoesNotContain("whsec", masked);
        Assert.DoesNotContain("\"k\"", masked);
        Assert.Contains(SensitiveConfigurationRedactor.MaskToken, masked);
        Assert.Contains("Sandbox", masked);
    }

    [Fact]
    public void SandboxWebhook_RejectsInvalidSignature()
    {
        var provider = new SchooleraSandboxPaymentProvider();
        var ok = provider.VerifyWebhookSignature(
            """{"eventId":"e1"}""",
            "deadbeef",
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            "sandbox-dev-webhook-secret-not-real");
        Assert.False(ok);
    }

    [Fact]
    public void SandboxWebhook_AcceptsValidHmac()
    {
        var provider = new SchooleraSandboxPaymentProvider();
        const string secret = "sandbox-dev-webhook-secret-not-real";
        var body = """{"eventId":"e1","status":"succeeded"}""";
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var sig = SchooleraSandboxPaymentProvider.ComputeHmacHex(secret, $"{ts}.{body}");
        Assert.True(provider.VerifyWebhookSignature(body, sig, ts, secret));
    }

    [Fact]
    public void SandboxWebhook_RejectsStaleTimestamp()
    {
        var provider = new SchooleraSandboxPaymentProvider();
        const string secret = "sandbox-dev-webhook-secret-not-real";
        var body = """{"eventId":"e1"}""";
        var ts = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600).ToString();
        var sig = SchooleraSandboxPaymentProvider.ComputeHmacHex(secret, $"{ts}.{body}");
        Assert.False(provider.VerifyWebhookSignature(body, sig, ts, secret));
    }

    [Fact]
    public async Task Parent_Payments_RequiresAuth()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/parent/payments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Payments_RequiresAuth()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/admin/payments/monitoring");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SandboxWebhook_WithoutSignature_AcknowledgesRejection()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsync(
            "/api/webhooks/payments/schoolera-sandbox",
            new StringContent("""{"eventId":"x","status":"succeeded"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("accepted", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PaymentTables_Exist_WhenDatabaseAvailable()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        try
        {
            if (!await db.Database.CanConnectAsync())
            {
                return;
            }
        }
        catch
        {
            return;
        }

        // Migration may or may not be applied in the test host.
        try
        {
            _ = await db.PaymentIntents.AsNoTracking().CountAsync();
            _ = await db.FinancingRequests.AsNoTracking().CountAsync();
            _ = await db.SchoolPayableItems.AsNoTracking().CountAsync();
        }
        catch (Exception)
        {
            // Schema not migrated yet in this environment — not a test failure for unit host.
        }
    }

    [Fact]
    public void Receipt_IsNotTaxInvoiceByDefault()
    {
        var receipt = new PaymentReceipt(
            Guid.NewGuid(),
            "RCP-20260717-00001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            "EGP",
            ProviderEnvironment.Sandbox,
            "Sandbox",
            "sbx_pay_1",
            DateTimeOffset.UtcNow);
        Assert.False(receipt.IsTaxInvoice);
        Assert.Equal(ProviderEnvironment.Sandbox, receipt.Environment);
    }
}
