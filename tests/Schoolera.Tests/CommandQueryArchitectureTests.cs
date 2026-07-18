using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Tests;

public sealed class CommandQueryArchitectureTests
{
    [Fact]
    public void CommandsAndQueries_ShouldHaveMatchingHandlersAndValidators()
    {
        foreach (var requestType in GetRequestTypes())
        {
            var handlerType = GetHandlerType(requestType);

            Assert.True(
                HandlesRequest(handlerType, requestType),
                $"{handlerType.Name} must implement IRequestHandler for {requestType.Name}.");

            if (!IsQuery(requestType))
            {
                var validatorType = GetValidatorType(requestType);
                Assert.True(
                    typeof(IValidator<>).MakeGenericType(requestType).IsAssignableFrom(validatorType),
                    $"{validatorType.Name} must implement IValidator<{requestType.Name}>.");
            }
        }
    }

    [Fact]
    public void CommandAndQueryHandlers_ShouldInjectTypedLogger()
    {
        foreach (var requestType in GetRequestTypes().Where(type => !IsQuery(type)))
        {
            var handlerType = GetHandlerType(requestType);
            var expectedLoggerType = typeof(ILogger<>).MakeGenericType(handlerType);
            var hasTypedLogger = handlerType.GetConstructors()
                .Any(constructor => constructor.GetParameters()
                    .Any(parameter => parameter.ParameterType == expectedLoggerType));

            Assert.True(
                hasTypedLogger,
                $"{handlerType.Name} must inject ILogger<{handlerType.Name}>.");
        }
    }

    [Fact]
    public void CommandHandlers_ShouldInjectUnitOfWork()
    {
        foreach (var requestType in GetRequestTypes().Where(IsCommand).Where(IsPersistenceCommand))
        {
            var handlerType = GetHandlerType(requestType);
            var hasUnitOfWork = handlerType.GetConstructors()
                .Any(constructor => constructor.GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(IUnitOfWork)));

            Assert.True(
                hasUnitOfWork,
                $"{handlerType.Name} must inject IUnitOfWork and commit changes after repository writes.");
        }
    }

    [Fact]
    public void QueryHandlers_ShouldNotInjectUnitOfWork()
    {
        foreach (var requestType in GetRequestTypes().Where(IsQuery))
        {
            var handlerType = GetHandlerType(requestType);
            var hasUnitOfWork = handlerType.GetConstructors()
                .Any(constructor => constructor.GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(IUnitOfWork)));

            Assert.False(
                hasUnitOfWork,
                $"{handlerType.Name} should not inject IUnitOfWork because queries must not commit changes.");
        }
    }

    [Fact]
    public void CommandsAndQueries_ShouldUseHandlerAndValidatorFilesOnly()
    {
        foreach (var requestType in GetRequestTypes())
        {
            var folder = GetRequestFolder(requestType);
            var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var expectedFiles = IsQuery(requestType)
                ? BuildQueryExpectedFiles(folder, requestType.Name)
                : new[]
                {
                    $"{requestType.Name}Handler.cs",
                    $"{requestType.Name}Validator.cs",
                };
            expectedFiles = expectedFiles.Order(StringComparer.Ordinal).ToArray();

            Assert.Equal(expectedFiles, files);
        }
    }

    private static string[] BuildQueryExpectedFiles(string folder, string requestName)
    {
        var expected = new List<string> { $"{requestName}Handler.cs" };
        if (File.Exists(Path.Combine(folder, $"{requestName}Validator.cs")))
        {
            expected.Add($"{requestName}Validator.cs");
        }

        return expected.ToArray();
    }

    [Fact]
    public void CommandsAndQueries_ShouldBeDeclaredInHandlerFiles()
    {
        foreach (var requestType in GetRequestTypes())
        {
            var folder = GetRequestFolder(requestType);
            var standaloneRequestFile = Path.Combine(folder, $"{requestType.Name}.cs");
            var handlerFile = Path.Combine(folder, $"{requestType.Name}Handler.cs");
            var handlerSource = File.ReadAllText(handlerFile);

            Assert.False(
                File.Exists(standaloneRequestFile),
                $"{requestType.Name} must be declared in {requestType.Name}Handler.cs, not a separate request file.");
            Assert.Contains($"record {requestType.Name}", handlerSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CommandsAndQueries_ShouldLiveUnderCommandsOrQueriesFolders()
    {
        foreach (var requestType in GetRequestTypes())
        {
            Assert.NotNull(requestType.Namespace);
            Assert.True(
                IsCommand(requestType) || IsQuery(requestType),
                $"{requestType.Name} must live under a Commands or Queries namespace.");
        }
    }

    private static IReadOnlyCollection<Type> GetRequestTypes()
    {
        var requests = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => !type.IsAbstract)
            .Where(type => type.Namespace is not null)
            .Where(type => IsRequest(type))
            .ToArray();

        Assert.NotEmpty(requests);

        return requests;
    }

    private static bool IsRequest(Type type)
    {
        return type.GetInterfaces().Any(interfaceType =>
            interfaceType == typeof(IRequest) ||
            (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRequest<>)));
    }

    private static bool IsCommand(Type type)
    {
        return type.Namespace?.Contains(".Commands.", StringComparison.Ordinal) == true;
    }

    private static bool IsQuery(Type type)
    {
        return type.Namespace?.Contains(".Queries.", StringComparison.Ordinal) == true;
    }

    private static bool IsPersistenceCommand(Type type)
    {
        return type.Namespace?.Contains(".Auth.Commands.", StringComparison.Ordinal) != true;
    }

    private static Type GetHandlerType(Type requestType)
    {
        return FindType(requestType, $"{requestType.Name}Handler")
            ?? throw new InvalidOperationException($"{requestType.Name} must have a matching handler.");
    }

    private static Type GetValidatorType(Type requestType)
    {
        if (IsQuery(requestType))
        {
            throw new InvalidOperationException($"{requestType.Name} is a query and does not require a validator.");
        }

        return FindType(requestType, $"{requestType.Name}Validator")
            ?? throw new InvalidOperationException($"{requestType.Name} must have a matching validator.");
    }

    private static Type? FindType(Type requestType, string typeName)
    {
        return requestType.Assembly.GetTypes()
            .FirstOrDefault(type =>
                type.Namespace == requestType.Namespace &&
                type.Name == typeName);
    }

    private static bool HandlesRequest(Type handlerType, Type requestType)
    {
        return handlerType.GetInterfaces().Any(interfaceType =>
        {
            if (!interfaceType.IsGenericType)
            {
                return false;
            }

            var definition = interfaceType.GetGenericTypeDefinition();
            if (definition == typeof(IRequestHandler<,>))
            {
                return interfaceType.GetGenericArguments()[0] == requestType;
            }

            if (definition == typeof(IRequestHandler<>))
            {
                return interfaceType.GetGenericArguments()[0] == requestType;
            }

            return false;
        });
    }

    private static string GetRequestFolder(Type requestType)
    {
        var applicationRoot = Path.Combine(FindRepositoryRoot(), "src", "Schoolera.Application");
        var relativeNamespace = requestType.Namespace!
            .Replace("Schoolera.Application.", string.Empty, StringComparison.Ordinal)
            .Replace('.', Path.DirectorySeparatorChar);

        return Path.Combine(applicationRoot, relativeNamespace);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Schoolera.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the Schoolera repository root.");
    }
}