namespace Schoolera.Api.Options;

public sealed class NswagOptions
{
    public const string SectionName = "Nswag";

    public bool Enabled { get; set; }

    /// <summary>
    /// Optional absolute Swagger URL. When empty, resolved from the running server address.
    /// </summary>
    public string? SwaggerUrl { get; set; }

    public string SwaggerPath { get; set; } = "/swagger/v1/swagger.json";

    public string OutputPath { get; set; } =
        "Schoolera-SPA/src/app/core/api-client/SwaggerClient.service.ts";
}
