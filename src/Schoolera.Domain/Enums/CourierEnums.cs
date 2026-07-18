namespace Schoolera.Domain.Enums;

public enum CourierServiceType
{
    HomePickup = 1,
}

public enum CourierCoverageResult
{
    Covered = 1,
    Unavailable = 2,
}

public enum CourierProviderEnvironment
{
    Simulated = 1,
    Sandbox = 2,
    Production = 3,
}

public enum SimulatedCourierHealthScenario
{
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
}

public enum SimulatedCourierAvailabilityScenario
{
    Available = 1,
    Unavailable = 2,
}
