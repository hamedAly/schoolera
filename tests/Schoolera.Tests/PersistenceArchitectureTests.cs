using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure;
using Schoolera.Infrastructure.Persistence;
using Schoolera.Infrastructure.Persistence.Repositories;

namespace Schoolera.Tests;

public sealed class PersistenceArchitectureTests
{
    [Fact]
    public void UnitOfWork_ShouldBeConcreteInfrastructureImplementation()
    {
        Assert.True(typeof(IUnitOfWork).IsAssignableFrom(typeof(UnitOfWork)));
        Assert.False(typeof(IUnitOfWork).IsAssignableFrom(typeof(SchooleraDbContext)));
    }

    [Fact]
    public void Infrastructure_ShouldRegisterEfPersistenceAsScoped()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\MSSQLLocalDB;Database=Schoolera_Test;Trusted_Connection=True;TrustServerCertificate=True"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(SchooleraDbContext) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IUnitOfWork) &&
            descriptor.ImplementationType == typeof(UnitOfWork) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ISchoolRepository) &&
            descriptor.ImplementationType == typeof(SchoolRepository) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Infrastructure_ShouldRequireDefaultConnectionString()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddInfrastructure(configuration));

        Assert.Contains("DefaultConnection", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistenceImplementations_ShouldLiveUnderInfrastructurePersistence()
    {
        Assert.StartsWith(
            "Schoolera.Infrastructure.Persistence",
            typeof(UnitOfWork).Namespace,
            StringComparison.Ordinal);
        Assert.StartsWith(
            "Schoolera.Infrastructure.Persistence",
            typeof(SchoolRepository).Namespace,
            StringComparison.Ordinal);
        Assert.StartsWith(
            "Schoolera.Infrastructure.Persistence",
            typeof(SchooleraDbContext).Namespace,
            StringComparison.Ordinal);
    }
}