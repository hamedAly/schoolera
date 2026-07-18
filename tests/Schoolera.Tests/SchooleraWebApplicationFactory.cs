using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Schoolera.Tests;

/// <summary>
/// Integration-test host that redirects private document storage to a temp directory
/// so admission/child upload fixtures never write into <c>src/Schoolera.Api/App_Data/private</c>.
/// </summary>
public sealed class SchooleraWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly string SharedPrivateRoot = Path.Combine(
        Path.GetTempPath(),
        "schoolera-private-tests",
        "integration-shared");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(SharedPrivateRoot);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PrivateFileStorage:StorageRoot"] = SharedPrivateRoot,
            });
        });
    }
}
