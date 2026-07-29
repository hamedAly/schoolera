namespace Schoolera.E2E.Infrastructure;

[CollectionDefinition(E2ECollection.Name)]
public sealed class E2ECollection : ICollectionFixture<AuthStateFixture>
{
    public const string Name = "SchooleraE2E";
}

/// <summary>
/// Creates cookie storage-state files once per test collection for all seed roles.
/// </summary>
public sealed class AuthStateFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Fail fast if password is missing before launching browsers.
        _ = E2EConfig.SeedPassword;
        _ = E2EConfig.BaseUrl;

        await AuthHelper.EnsureStorageStatesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
