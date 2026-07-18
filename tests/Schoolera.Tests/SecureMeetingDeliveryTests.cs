using Schoolera.Application.Integrations;
using Schoolera.Application.Meetings;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Meetings;

namespace Schoolera.Tests;

public sealed class SecureMeetingDeliveryTests
{
    private const string ValidSettings =
        """
        {
          "environment": "Development",
          "publicNameAr": "اجتماع تجريبي",
          "publicNameEn": "Simulated meeting",
          "maximumDurationMinutes": 60,
          "joinBeforeMinutes": 15,
          "joinAfterMinutes": 5,
          "hostBeforeMinutes": 30,
          "requestTimeoutSeconds": 10,
          "maxRetryAttempts": 3,
          "retryDelaySeconds": 5,
          "supportsMeetingCreation": true,
          "supportsParentAccess": true,
          "supportsHostAccess": true,
          "supportsCancellation": true,
          "supportsStatusQuery": true,
          "simulatedScenario": "Success"
        }
        """;

    [Fact]
    public void Meeting_settings_accept_bounded_development_simulator()
    {
        var result = new IntegrationSettingsValidator().Validate(
            IntegrationType.Meeting, IntegrationProviderCodes.Simulated, ValidSettings, 1);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Meeting_settings_reject_simulated_production_environment()
    {
        var json = ValidSettings.Replace("Development", "Production");
        var result = new IntegrationSettingsValidator().Validate(
            IntegrationType.Meeting, IntegrationProviderCodes.Simulated, json, 1);
        Assert.False(result.IsValid);
        Assert.Contains("integrations.meeting.simulatedDevelopmentOnly", result.ErrorCodes);
    }

    [Fact]
    public void Meeting_settings_reject_unimplemented_real_provider()
    {
        var result = new IntegrationSettingsValidator().Validate(
            IntegrationType.Meeting, "GuessedProvider", ValidSettings, 1);
        Assert.False(result.IsValid);
        Assert.Contains("integrations.providerNotConfigured", result.ErrorCodes);
    }

    [Fact]
    public void Meeting_secret_fields_are_masked()
    {
        var masked = SensitiveConfigurationRedactor.MaskJson(
            """{"ApiKey":"key","ApiSecret":"secret","AccessToken":"token","WebhookSecret":"hook"}""");
        Assert.DoesNotContain("\"key\"", masked);
        Assert.DoesNotContain("\"secret\"", masked);
        Assert.DoesNotContain("\"token\"", masked);
        Assert.DoesNotContain("\"hook\"", masked);
    }

    [Fact]
    public void Pending_session_transitions_to_ready_and_preserves_historical_reference()
    {
        var session = NewSession();
        session.RecordProvisioningAttempt();
        session.MarkReady("sim-reference", "meeting.simulated.ready");
        Assert.Equal(MeetingSessionStatus.Ready, session.Status);
        Assert.Equal("sim-reference", session.ProviderMeetingReference);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            session.IntegrationConfigurationId);
        Assert.Equal(1, session.ProvisioningAttemptCount);
    }

    [Fact]
    public void Failed_session_requires_explicit_retry_transition()
    {
        var session = NewSession();
        session.RecordProvisioningAttempt();
        session.MarkProvisioningFailed(
            "meeting.simulated.retryableFailure", true, DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(MeetingSessionStatus.ProvisioningFailed, session.Status);
        session.BeginProvisioningRetry();
        Assert.Equal(MeetingSessionStatus.PendingProvisioning, session.Status);
    }

    [Fact]
    public void Pending_session_cancels_without_provider_call()
    {
        var session = NewSession();
        session.RequestCancellation(providerCancellationRequired: false);
        Assert.Equal(MeetingSessionStatus.Cancelled, session.Status);
        Assert.True(session.IsTerminal);
    }

    [Fact]
    public void Ready_session_uses_cancellation_pending_and_cannot_regress_after_cancelled()
    {
        var session = NewSession();
        session.RecordProvisioningAttempt();
        session.MarkReady("sim-reference", "meeting.simulated.ready");
        session.RequestCancellation(providerCancellationRequired: true);
        session.RecordCancellationAttempt();
        session.MarkCancellationSucceeded("meeting.simulated.cancelled");
        session.RequestCancellation(providerCancellationRequired: true);
        Assert.Equal(MeetingSessionStatus.Cancelled, session.Status);
    }

    [Fact]
    public async Task Simulated_provider_returns_deterministic_non_secret_internal_access()
    {
        var provider = new SimulatedMeetingProvider(new FakeRuntimeEnvironment(true, false));
        var context = NewContext();
        var provisioned = await provider.ProvisionAsync(context);
        var parent = await provider.ResolveParentAccessAsync(context);
        var host = await provider.ResolveHostAccessAsync(context);
        Assert.Equal(MeetingProviderOperationResult.Succeeded, provisioned.Result);
        Assert.StartsWith("sim-", provisioned.SafeProviderMeetingReference);
        Assert.StartsWith("/dev/simulated-meeting/", parent!.ActionPath);
        Assert.EndsWith("/parent", parent.ActionPath);
        Assert.EndsWith("/host", host!.ActionPath);
    }

    [Fact]
    public async Task Simulated_provider_is_blocked_outside_development_and_test()
    {
        var provider = new SimulatedMeetingProvider(new FakeRuntimeEnvironment(false, false));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ProvisionAsync(NewContext()));
    }

    [Theory]
    [InlineData("Success", MeetingProviderOperationResult.Succeeded)]
    [InlineData("Pending", MeetingProviderOperationResult.Pending)]
    [InlineData("RetryableFailure", MeetingProviderOperationResult.RetryableFailure)]
    [InlineData("PermanentFailure", MeetingProviderOperationResult.PermanentFailure)]
    public async Task Simulated_provider_has_deterministic_provisioning_scenarios(
        string scenario, MeetingProviderOperationResult expected)
    {
        var provider = new SimulatedMeetingProvider(new FakeRuntimeEnvironment(true, false));
        var result = await provider.ProvisionAsync(NewContext(scenario));
        Assert.Equal(expected, result.Result);
        Assert.DoesNotContain("http", result.SafeCode, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Success", MeetingProviderOperationResult.Succeeded)]
    [InlineData("CancellationFailure", MeetingProviderOperationResult.RetryableFailure)]
    public async Task Simulated_provider_has_deterministic_cancellation_scenarios(
        string scenario, MeetingProviderOperationResult expected)
    {
        var provider = new SimulatedMeetingProvider(new FakeRuntimeEnvironment(true, false));
        var result = await provider.CancelAsync(NewContext(scenario), "sim-reference");
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void Pending_session_can_remain_pending_for_bounded_worker_retry()
    {
        var session = NewSession();
        session.RecordProvisioningAttempt();
        var retryAt = DateTimeOffset.UtcNow.AddMinutes(1);
        session.KeepProvisioningPending("meeting.simulated.pending", retryAt);
        Assert.Equal(MeetingSessionStatus.PendingProvisioning, session.Status);
        Assert.Equal(retryAt, session.NextRetryAtUtc);
        Assert.Null(session.LastSafeFailureCode);
    }

    [Fact]
    public void Ready_session_expires_terminally()
    {
        var session = NewSession();
        session.RecordProvisioningAttempt();
        session.MarkReady("sim-reference", "meeting.simulated.ready");
        session.MarkExpired();
        session.RequestCancellation(providerCancellationRequired: true);
        Assert.Equal(MeetingSessionStatus.Expired, session.Status);
        Assert.True(session.IsTerminal);
    }

    private static AdmissionMeetingSession NewSession()
    {
        var starts = DateTimeOffset.UtcNow.AddHours(1);
        return new(
            Guid.NewGuid(), Guid.NewGuid(), SlotKind.Interview, Guid.NewGuid(), Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            IntegrationProviderCodes.Simulated, MeetingProviderEnvironment.Development,
            1, [1], 1, $"meeting:{Guid.NewGuid():N}", starts, starts.AddHours(1),
            starts.AddHours(1).AddMinutes(5));
    }

    private static MeetingProvisioningContext NewContext(string scenario = "Success")
    {
        var starts = DateTimeOffset.UtcNow;
        return new(
            Guid.NewGuid(), Guid.NewGuid(), $"meeting:{Guid.NewGuid():N}",
            starts, starts.AddHours(1),
            IntegrationSettingsSerializer.Deserialize<MeetingIntegrationSettings>(
                ValidSettings.Replace("\"Success\"", $"\"{scenario}\"")));
    }

    private sealed record FakeRuntimeEnvironment(bool IsDevelopment, bool IsTest)
        : IRuntimeEnvironment
    {
        public bool IsEnvironment(string environmentName) =>
            IsTest && string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase);
    }
}
