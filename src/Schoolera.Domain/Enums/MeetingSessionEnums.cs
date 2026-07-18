namespace Schoolera.Domain.Enums;

public enum MeetingProviderEnvironment
{
    Development = 1,
    Test = 2,
    Sandbox = 3,
    Production = 4,
}

[Flags]
public enum MeetingProviderCapabilities
{
    None = 0,
    CreateMeeting = 1,
    ParentAccess = 2,
    HostAccess = 4,
    CancelMeeting = 8,
    QueryStatus = 16,
}

public enum MeetingSessionStatus
{
    PendingProvisioning = 1,
    Ready = 2,
    ProvisioningFailed = 3,
    CancellationPending = 4,
    Cancelled = 5,
    Expired = 6,
}

public enum MeetingSessionHistoryAction
{
    ProvisioningRequested = 1,
    ProvisioningSucceeded = 2,
    ProvisioningFailed = 3,
    RetryRequested = 4,
    ParentAccessIssued = 5,
    HostAccessIssued = 6,
    CancellationRequested = 7,
    CancellationSucceeded = 8,
    CancellationFailed = 9,
    Expired = 10,
}

public enum SimulatedMeetingScenario
{
    Success = 1,
    Pending = 2,
    RetryableFailure = 3,
    PermanentFailure = 4,
    CancellationFailure = 5,
}
