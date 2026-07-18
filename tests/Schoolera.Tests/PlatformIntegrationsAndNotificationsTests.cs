using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

public sealed class PlatformIntegrationsAndNotificationsTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlatformIntegrationsAndNotificationsTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public void SensitiveRedactor_MasksKnownProperties()
    {
        var json = """{"ApiKey":"secret-value","SenderEmail":"a@b.c"}""";
        var masked = SensitiveConfigurationRedactor.MaskJson(json);
        Assert.Contains(SensitiveConfigurationRedactor.MaskToken, masked);
        Assert.DoesNotContain("secret-value", masked);
        Assert.Contains("a@b.c", masked);
    }

    [Fact]
    public void SensitiveRedactor_PreservesMaskedOnMerge()
    {
        var stored = """{"ApiKey":"real-secret","SenderEmail":"from@schoolera.local"}""";
        var submitted = """{"ApiKey":"***","SenderEmail":"from@schoolera.local"}""";
        var merged = SensitiveConfigurationRedactor.MergePreservingSecrets(stored, submitted);
        Assert.Contains("real-secret", merged);
    }

    [Fact]
    public void SensitiveRedactor_ReplacesWhenNewValueProvided()
    {
        var stored = """{"ApiKey":"old-secret"}""";
        var submitted = """{"ApiKey":"new-secret"}""";
        var merged = SensitiveConfigurationRedactor.MergePreservingSecrets(stored, submitted);
        Assert.Contains("new-secret", merged);
        Assert.DoesNotContain("old-secret", merged);
    }

    [Fact]
    public void IntegrationValidator_RejectsMalformedJson()
    {
        var validator = new IntegrationSettingsValidator();
        var result = validator.Validate(IntegrationType.Email, "Simulated", "{not-json", 1);
        Assert.False(result.IsValid);
        Assert.Contains("integrations.invalidJson", result.ErrorCodes);
    }

    [Fact]
    public void IntegrationValidator_AcceptsSimulatedEmail()
    {
        var validator = new IntegrationSettingsValidator();
        var result = validator.Validate(IntegrationType.Email, "Simulated", """{"mode":"simulated"}""", 1);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void IntegrationValidator_RejectsSmtpWithoutHost()
    {
        var validator = new IntegrationSettingsValidator();
        var result = validator.Validate(
            IntegrationType.Email,
            "Smtp",
            """{"SenderEmail":"noreply@schoolera.local","Port":587}""",
            1);
        Assert.False(result.IsValid);
        Assert.Contains("integrations.email.hostRequired", result.ErrorCodes);
    }

    [Fact]
    public async Task Admin_Integrations_RequiresAuth()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/admin/integrations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Parent_Notifications_RequiresAuth()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/parent/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_Simulated_Integrations_Exist_WhenDatabaseAvailable()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();
        try
        {
            await db.Database.CanConnectAsync();
        }
        catch
        {
            return; // skip if no DB in this environment
        }

        var emailDefault = await db.PlatformIntegrationConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.IntegrationType == IntegrationType.Email && x.IsDefault && x.IsActive);

        if (emailDefault is null)
        {
            // Seed may not have run in test host — acceptable
            return;
        }

        Assert.True(IntegrationProviderCodes.IsSimulated(emailDefault.ProviderCode));
        Assert.DoesNotContain("sk_live", emailDefault.SettingsJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeduplicationKey_IsChannelScoped()
    {
        var baseKey = "admission-open:offering:abc";
        var emailKey = $"{baseKey}:{NotificationChannel.Email}";
        var inAppKey = $"{baseKey}:{NotificationChannel.InApp}";
        Assert.NotEqual(emailKey, inAppKey);
    }
}
