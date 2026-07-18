using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Email;

namespace Schoolera.Tests;

[Collection(WebApplicationFactoryCollection.Name)]
public sealed class VerificationFlowTests : IClassFixture<SchooleraWebApplicationFactory>
{
    private readonly SchooleraWebApplicationFactory _factory;

    public VerificationFlowTests(SchooleraWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_IssuesCode_AndDevelopmentLogDelivers()
    {
        var recordingSender = new RecordingEmailSender();
        using var client = CreateClient(recordingSender);
        await EnsureAntiforgeryAsync(client);

        var email = $"owner-{Guid.NewGuid():N}@schoolera.test";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register/school-owner",
            CreateRegisterBody(email));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var data = json.GetProperty("data");
        Assert.True(data.GetProperty("requiresVerification").GetBoolean());
        Assert.True(data.GetProperty("verificationDeliverySucceeded").GetBoolean());
        Assert.Equal(EmailDeliveryModes.DevelopmentLog, data.GetProperty("verificationDeliveryMode").GetString());
        Assert.Equal(1, recordingSender.SentCount);
        Assert.Equal(email, recordingSender.LastRecipient, StringComparer.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(recordingSender.LastCode));
        Assert.DoesNotContain(recordingSender.LastCode!, json.ToString());
    }

    [Fact]
    public async Task Verify_WithCorrectCode_ConfirmsEmail_AndRejectsReuse()
    {
        var recordingSender = new RecordingEmailSender();
        using var client = CreateClient(recordingSender);
        await EnsureAntiforgeryAsync(client);

        var email = $"parent-{Guid.NewGuid():N}@schoolera.test";
        var register = await client.PostAsJsonAsync("/api/auth/register/parent", CreateRegisterBody(email));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var code = recordingSender.LastCode!;

        var verify = await client.PostAsJsonAsync("/api/auth/verify", new { email, code });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);

        var reuse = await client.PostAsJsonAsync("/api/auth/verify", new { email, code });
        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        var reuseJson = await reuse.Content.ReadFromJsonAsync<JsonElement>();
        var codes = reuseJson.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Assert.Contains(codes, codeValue =>
            codeValue is "auth.codeAlreadyUsed" or "auth.emailAlreadyVerified" or "auth.invalidVerificationCode");
    }

    [Fact]
    public async Task Verify_WithWrongCode_ReturnsInvalidCode()
    {
        var recordingSender = new RecordingEmailSender();
        using var client = CreateClient(recordingSender);
        await EnsureAntiforgeryAsync(client);

        var email = $"wrong-{Guid.NewGuid():N}@schoolera.test";
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/auth/register/parent", CreateRegisterBody(email))).StatusCode);

        var verify = await client.PostAsJsonAsync("/api/auth/verify", new { email, code = "000000" });
        Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
        var json = await verify.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("auth.invalidVerificationCode",
            json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!));
    }

    [Fact]
    public async Task Verify_WithEightDigitCode_ReturnsValidationError()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        await EnsureAntiforgeryAsync(client);

        var verify = await client.PostAsJsonAsync(
            "/api/auth/verify",
            new { email = "anyone@schoolera.test", code = "12345678" });

        Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
        var json = await verify.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("error.validation",
            json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!));
    }

    [Fact]
    public async Task Resend_InvalidatesPreviousCode_AndDeliversNewOne()
    {
        var recordingSender = new RecordingEmailSender();
        using var client = CreateClient(recordingSender, resendCooldownSeconds: 0);
        await EnsureAntiforgeryAsync(client);

        var email = $"resend-{Guid.NewGuid():N}@schoolera.test";
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/auth/register/parent", CreateRegisterBody(email))).StatusCode);
        var firstCode = recordingSender.LastCode!;

        var resend = await client.PostAsJsonAsync("/api/auth/resend-verification", new { email });
        Assert.Equal(HttpStatusCode.OK, resend.StatusCode);
        var secondCode = recordingSender.LastCode!;
        Assert.NotEqual(firstCode, secondCode);

        var oldVerify = await client.PostAsJsonAsync("/api/auth/verify", new { email, code = firstCode });
        Assert.Equal(HttpStatusCode.BadRequest, oldVerify.StatusCode);

        var newVerify = await client.PostAsJsonAsync("/api/auth/verify", new { email, code = secondCode });
        Assert.Equal(HttpStatusCode.OK, newVerify.StatusCode);
    }

    [Fact]
    public async Task Resend_WithinCooldown_ReturnsResendTooSoon()
    {
        var recordingSender = new RecordingEmailSender();
        using var client = CreateClient(recordingSender, resendCooldownSeconds: 60);
        await EnsureAntiforgeryAsync(client);

        var email = $"cooldown-{Guid.NewGuid():N}@schoolera.test";
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/api/auth/register/parent", CreateRegisterBody(email))).StatusCode);

        var resend = await client.PostAsJsonAsync("/api/auth/resend-verification", new { email });
        Assert.Equal(HttpStatusCode.BadRequest, resend.StatusCode);
        var json = await resend.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("auth.resendTooSoon",
            json.GetProperty("errorCodes").EnumerateArray().Select(item => item.GetString()!));
    }

    [Fact]
    public async Task Verify_WithoutCsrf_IsRejected()
    {
        using var client = AuthTestHelpers.CreateCookieClient(_factory);
        // Hit me without applying XSRF header.
        _ = await client.GetAsync("/api/auth/me");

        var response = await client.PostAsJsonAsync(
            "/api/auth/verify",
            new { email = "x@y.com", code = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient(RecordingEmailSender recordingSender, int resendCooldownSeconds = 60)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Verification:ResendCooldownSeconds"] = resendCooldownSeconds.ToString(),
                    ["Email:Mode"] = EmailDeliveryModes.DevelopmentLog,
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionalEmailSender>();
                services.AddSingleton<ITransactionalEmailSender>(recordingSender);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    private static async Task EnsureAntiforgeryAsync(HttpClient client)
    {
        var me = await client.GetAsync("/api/auth/me");
        AuthTestHelpers.ApplyAntiforgeryFromResponse(client, me);
    }

    private static object CreateRegisterBody(string email) => new
    {
        firstName = "Test",
        lastName = "User",
        email,
        phoneNumber = $"+201{Random.Shared.NextInt64(100000000, 999999999)}",
        password = "Schoolera@Dev1",
        confirmPassword = "Schoolera@Dev1",
        termsAccepted = true,
        privacyAccepted = true,
        preferredLanguage = "ar",
    };

    private sealed class RecordingEmailSender : ITransactionalEmailSender
    {
        public string Mode => EmailDeliveryModes.DevelopmentLog;
        public int SentCount { get; private set; }
        public string? LastRecipient { get; private set; }
        public string? LastCode { get; private set; }

        public Task SendVerificationCodeAsync(
            VerificationEmailMessage message,
            CancellationToken cancellationToken = default)
        {
            SentCount++;
            LastRecipient = message.RecipientEmail;
            LastCode = message.VerificationCode;
            return Task.CompletedTask;
        }
    }
}
