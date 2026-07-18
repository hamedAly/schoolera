using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schoolera.Infrastructure;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeDatabaseAsync_WhenMigrationsDisabled_DoesNotThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\MSSQLLocalDB;Database=Schoolera_Init_Test;Trusted_Connection=True;TrustServerCertificate=True",
                ["Database:ApplyMigrations"] = "false",
                ["Database:SeedData"] = "false",
                ["FileStorage:StorageRoot"] = Path.Combine(Path.GetTempPath(), "schoolera-db-init-uploads"),
                ["FileStorage:PublicRequestPath"] = "/uploads",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddInfrastructure(configuration, new TestHostEnvironment());

        await using var provider = services.BuildServiceProvider();
        await provider.InitializeDatabaseAsync();
    }

    [Fact]
    public void DatabaseOptions_BindFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrations"] = "false",
                ["Database:SeedData"] = "true",
            })
            .Build();

        var options = new DatabaseOptions();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(options);

        Assert.False(options.ApplyMigrations);
        Assert.True(options.SeedData);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Schoolera.Tests";

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
