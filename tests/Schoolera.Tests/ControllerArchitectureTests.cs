using System.Reflection;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Schoolera.Api.Controllers;
using Schoolera.Application.Common.Models;

namespace Schoolera.Tests;

public sealed class ControllerArchitectureTests
{
    [Fact]
    public void ControllerActions_ShouldReturnResult()
    {
        foreach (var action in GetControllerActions())
        {
            var returnType = UnwrapTask(action.ReturnType);

            Assert.True(
                IsResultType(returnType),
                $"{action.DeclaringType?.Name}.{action.Name} must return Result<T> or Task<Result<T>>.");
        }
    }

    [Fact]
    public void Controllers_ShouldInjectMediatorAndLogger()
    {
        foreach (var controller in GetControllerTypes())
        {
            var constructors = controller.GetConstructors();
            var hasMediator = constructors.Any(constructor =>
                constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ISender)));
            var hasLogger = constructors.Any(constructor =>
                constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ILogger<>).MakeGenericType(controller)));

            Assert.True(hasMediator, $"{controller.Name} must inject ISender mediator.");
            Assert.True(hasLogger, $"{controller.Name} must inject ILogger<{controller.Name}>.");
        }
    }
    [Fact]
    public void Controllers_ShouldNotInjectOrCallRepositories()
    {
        foreach (var controller in GetControllerTypes())
        {
            var repositoryParameter = controller.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .FirstOrDefault(parameter =>
                    parameter.ParameterType.Name.Contains("Repository", StringComparison.OrdinalIgnoreCase));

            Assert.Null(repositoryParameter);
        }

        foreach (var file in GetControllerFiles())
        {
            var source = File.ReadAllText(file);

            Assert.DoesNotContain("Repository", source, StringComparison.OrdinalIgnoreCase);
        }
    }
    [Fact]
    public void BaseController_ShouldExposeResultHelpers()
    {
        var methods = typeof(ApiControllerBase)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic);

        Assert.Contains(methods, method =>
            method.Name == "Success" &&
            method.IsGenericMethodDefinition);

        Assert.Contains(methods, method =>
            method.Name == "Failure" &&
            method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == typeof(string));

        Assert.Contains(methods, method =>
            method.Name == "Failure" &&
            method.IsGenericMethodDefinition &&
            method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == typeof(IEnumerable<string>));
    }


    [Fact]
    public void Controllers_ShouldNotContainValidation()
    {
        var forbiddenTokens = new[]
        {
            "FluentValidation",
            "AbstractValidator",
            "ValidationException",
            "ModelState",
            "TryValidateModel",
            "ValidateAsync",
            "RuleFor(",
            ".NotEmpty(",
            ".MaximumLength("
        };

        foreach (var file in GetControllerFiles())
        {
            var source = File.ReadAllText(file);

            foreach (var token in forbiddenTokens)
            {
                Assert.DoesNotContain(token, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Controllers_ShouldNotDefineDtosRequestsOrResponses()
    {
        var dtoDefinitionPattern = new Regex(
            @"\b(class|record|struct)\s+\w*(Dto|Request|Response)\b",
            RegexOptions.Compiled);

        foreach (var file in GetControllerFiles())
        {
            var source = File.ReadAllText(file);

            Assert.False(
                dtoDefinitionPattern.IsMatch(source),
                $"{Path.GetFileName(file)} must not define DTO, request, or response models.");
        }
    }

    [Fact]
    public void Controllers_ShouldStayThin()
    {
        var forbiddenTokens = new[]
        {
            "Schoolera.Infrastructure",
            "Schoolera.Domain",
            "ISchoolRepository",
            "DbContext",
            "SaveChanges",
            "foreach (",
            "for (",
            "while (",
            "switch (",
            "if ("
        };

        foreach (var file in GetConcreteControllerFiles())
        {
            var source = File.ReadAllText(file);

            foreach (var token in forbiddenTokens)
            {
                Assert.DoesNotContain(token, source, StringComparison.Ordinal);
            }

            Assert.All(GetActionBodies(source), body =>
            {
                Assert.Contains("Mediator.Send", body, StringComparison.Ordinal);
                Assert.True(
                    CountMeaningfulLines(body) <= 8,
                    $"{Path.GetFileName(file)} action bodies should stay thin and delegate to MediatR.");
            });
        }
    }

    private static IReadOnlyCollection<Type> GetControllerTypes()
    {
        var controllers = typeof(ApiControllerBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract)
            .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Where(type => typeof(ApiControllerBase).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(controllers);

        return controllers;
    }

    private static IReadOnlyCollection<MethodInfo> GetControllerActions()
    {
        var actions = GetControllerTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToArray();

        Assert.NotEmpty(actions);

        return actions;
    }

    private static Type UnwrapTask(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    private static bool IsResultType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);
    }

    private static IReadOnlyCollection<string> GetControllerFiles()
    {
        var controllersPath = Path.Combine(FindRepositoryRoot(), "src", "Schoolera.Api", "Controllers");
        var files = Directory.GetFiles(controllersPath, "*.cs", SearchOption.TopDirectoryOnly);

        Assert.NotEmpty(files);

        return files;
    }

    private static IReadOnlyCollection<string> GetConcreteControllerFiles()
    {
        return GetControllerFiles()
            .Where(file => !Path.GetFileName(file).Equals("ApiControllerBase.cs", StringComparison.Ordinal))
            .ToArray();
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

    private static IEnumerable<string> GetActionBodies(string source)
    {
        var actionPattern = new Regex(
            @"\[Http(?:Get|Post|Put|Patch|Delete)[^\]]*\]\s*(?:\r?\n\s*\[[^\]]+\]\s*)*public\s+[^\{]+\{",
            RegexOptions.Compiled);

        foreach (Match match in actionPattern.Matches(source))
        {
            var bodyStart = match.Index + match.Length - 1;
            yield return ReadBraceBlock(source, bodyStart);
        }
    }

    private static string ReadBraceBlock(string source, int openingBraceIndex)
    {
        var depth = 0;

        for (var index = openingBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return source.Substring(openingBraceIndex, index - openingBraceIndex + 1);
                }
            }
        }

        throw new InvalidOperationException("Could not parse controller action body.");
    }

    private static int CountMeaningfulLines(string source)
    {
        return source
            .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .Count(line =>
                line.Length > 0 &&
                line != "{" &&
                line != "}" &&
                !line.StartsWith("//", StringComparison.Ordinal));
    }
}