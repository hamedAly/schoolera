using Microsoft.Playwright;

namespace Schoolera.E2E.Infrastructure;

/// <summary>
/// Authenticated page test that loads a role-specific Playwright storage state.
/// Requires <see cref="AuthStateFixture"/> via the E2E collection.
/// </summary>
[Collection(E2ECollection.Name)]
public abstract class AuthenticatedPageTest : SchooleraPageTest
{
    protected abstract SeedRole Role { get; }

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = SeedUsers.StorageStatePath(Role);
        return options;
    }
}

public abstract class ParentAuthTest : AuthenticatedPageTest
{
    protected override SeedRole Role => SeedRole.Parent;
}

public abstract class SchoolOwnerAuthTest : AuthenticatedPageTest
{
    protected override SeedRole Role => SeedRole.SchoolOwner;
}

public abstract class SchoolAdminAuthTest : AuthenticatedPageTest
{
    protected override SeedRole Role => SeedRole.SchoolAdmin;
}

public abstract class AdminAuthTest : AuthenticatedPageTest
{
    protected override SeedRole Role => SeedRole.PlatformAdmin;
}

public abstract class SupportAuthTest : AuthenticatedPageTest
{
    protected override SeedRole Role => SeedRole.SupportAgent;
}
