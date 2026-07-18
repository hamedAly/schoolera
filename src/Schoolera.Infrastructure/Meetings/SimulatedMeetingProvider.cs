using Microsoft.Extensions.Hosting;
using Schoolera.Application.Meetings;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Meetings;

public sealed class RuntimeEnvironment(IHostEnvironment environment) : IRuntimeEnvironment
{
    public bool IsDevelopment => environment.IsDevelopment();
    public bool IsEnvironment(string environmentName) => environment.IsEnvironment(environmentName);
}

public sealed class SimulatedMeetingProvider(IRuntimeEnvironment runtimeEnvironment) : IMeetingProvider
{
    public string ProviderCode => IntegrationProviderCodes.Simulated;
    public bool IsSimulated => true;

    public Task<MeetingProviderResult> ProvisionAsync(
        MeetingProvisioningContext context, CancellationToken cancellationToken = default)
    {
        EnsureAllowed();
        cancellationToken.ThrowIfCancellationRequested();
        var scenario = ParseScenario(context.Settings.SimulatedScenario);
        var result = scenario switch
        {
            SimulatedMeetingScenario.Success => new MeetingProviderResult(
                MeetingProviderOperationResult.Succeeded,
                "meeting.simulated.ready",
                $"sim-{context.MeetingSessionId:N}"),
            SimulatedMeetingScenario.Pending => new MeetingProviderResult(
                MeetingProviderOperationResult.Pending, "meeting.simulated.pending"),
            SimulatedMeetingScenario.RetryableFailure => new MeetingProviderResult(
                MeetingProviderOperationResult.RetryableFailure,
                "meeting.simulated.retryableFailure"),
            _ => new MeetingProviderResult(
                MeetingProviderOperationResult.PermanentFailure,
                "meeting.simulated.permanentFailure"),
        };
        return Task.FromResult(result);
    }

    public Task<MeetingProviderResult> CancelAsync(
        MeetingProvisioningContext context,
        string safeProviderMeetingReference,
        CancellationToken cancellationToken = default)
    {
        EnsureAllowed();
        cancellationToken.ThrowIfCancellationRequested();
        var scenario = ParseScenario(context.Settings.SimulatedScenario);
        return Task.FromResult(scenario == SimulatedMeetingScenario.CancellationFailure
            ? new MeetingProviderResult(
                MeetingProviderOperationResult.RetryableFailure,
                "meeting.simulated.cancellationFailure")
            : new MeetingProviderResult(
                MeetingProviderOperationResult.Succeeded,
                "meeting.simulated.cancelled"));
    }

    public Task<MeetingAccessActionDto?> ResolveParentAccessAsync(
        MeetingProvisioningContext context, CancellationToken cancellationToken = default)
    {
        EnsureAllowed();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<MeetingAccessActionDto?>(new(
            $"/dev/simulated-meeting/{context.MeetingSessionId}/parent",
            context.EndsAtUtc,
            true,
            "Parent"));
    }

    public Task<MeetingAccessActionDto?> ResolveHostAccessAsync(
        MeetingProvisioningContext context, CancellationToken cancellationToken = default)
    {
        EnsureAllowed();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<MeetingAccessActionDto?>(new(
            $"/dev/simulated-meeting/{context.MeetingSessionId}/host",
            context.EndsAtUtc,
            true,
            "Host"));
    }

    private void EnsureAllowed()
    {
        if (!runtimeEnvironment.IsDevelopment && !runtimeEnvironment.IsEnvironment("Test"))
            throw new InvalidOperationException("The simulated Meeting provider is Development/Test only.");
    }

    private static SimulatedMeetingScenario ParseScenario(string? value) =>
        Enum.TryParse<SimulatedMeetingScenario>(value, true, out var scenario)
            ? scenario
            : SimulatedMeetingScenario.PermanentFailure;
}
