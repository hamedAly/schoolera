using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using Schoolera.Api.Options;

namespace Schoolera.Api.Services;

public sealed class NSwagClientService(
    IHttpClientFactory httpClientFactory,
    IServer server,
    IWebHostEnvironment environment,
    IOptions<NswagOptions> options,
    ILogger<NSwagClientService> logger)
{
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogDebug("NSwag Angular client generation is disabled.");
            return;
        }

        var swaggerUrl = ResolveSwaggerUrl(settings);
        logger.LogInformation("Generating NSwag Angular client from {SwaggerUrl}.", swaggerUrl);

        var httpClient = httpClientFactory.CreateClient(nameof(NSwagClientService));
        var swaggerJson = await httpClient.GetStringAsync(swaggerUrl, cancellationToken);

        var document = await OpenApiDocument.FromJsonAsync(swaggerJson);
        var code = GenerateTypeScriptClient(document);

        var outputPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.OutputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        if (File.Exists(outputPath))
        {
            var existingCode = await File.ReadAllTextAsync(outputPath, cancellationToken);
            if (string.Equals(existingCode, code, StringComparison.Ordinal))
            {
                logger.LogInformation("NSwag Angular client is up to date at {OutputPath}.", outputPath);
                return;
            }
        }

        await File.WriteAllTextAsync(outputPath, code, cancellationToken);
        logger.LogInformation("NSwag Angular client generated at {OutputPath}.", outputPath);
    }

    private string ResolveSwaggerUrl(NswagOptions settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.SwaggerUrl))
        {
            return settings.SwaggerUrl;
        }

        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var baseAddress = addresses?
            .FirstOrDefault(address => address.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? addresses?.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Unable to resolve a server address for NSwag client generation.");

        return $"{baseAddress.TrimEnd('/')}{settings.SwaggerPath}";
    }

    private static string GenerateTypeScriptClient(OpenApiDocument document)
    {
        var settings = new TypeScriptClientGeneratorSettings
        {
            ClassName = "{controller}Client",
            GenerateClientClasses = true,
            GenerateDtoTypes = true,
            GenerateOptionalParameters = true,
            UseGetBaseUrlMethod = false,
            Template = TypeScriptTemplate.Angular,
            HttpClass = HttpClass.HttpClient,
            RxJsVersion = 7.0m,
            InjectionTokenType = InjectionTokenType.InjectionToken,
            BaseUrlTokenName = "API_BASE_URL",
            UseSingletonProvider = true,
            PromiseType = PromiseType.Promise,
            TypeScriptGeneratorSettings =
            {
                TypeStyle = TypeScriptTypeStyle.Interface,
                DateTimeType = TypeScriptDateTimeType.String,
            }
        };

        var generator = new TypeScriptClientGenerator(document, settings);
        return generator.GenerateFile();
    }
}
