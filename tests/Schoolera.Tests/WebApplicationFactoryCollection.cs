namespace Schoolera.Tests;

/// <summary>
/// Shares one <see cref="SchooleraWebApplicationFactory"/> across integration test classes
/// so database seeding does not race when xUnit runs fixtures in parallel.
/// </summary>
[CollectionDefinition(Name)]
public sealed class WebApplicationFactoryCollection : ICollectionFixture<SchooleraWebApplicationFactory>
{
    public const string Name = "WebApplicationFactory";
}
