namespace Schoolera.Application.Integrations.Constants;

/// <summary>Stable Platform Admin integration/notification error codes.</summary>
public static class IntegrationErrorCodes
{
    public const string Forbidden = "integrations.forbidden";
    public const string NotFound = "integrations.notFound";
    public const string InvalidJson = "integrations.invalidJson";
    public const string InvalidType = "integrations.invalidType";
    public const string InvalidProviderCode = "integrations.invalidProviderCode";
    public const string InvalidSchemaVersion = "integrations.invalidSchemaVersion";
    public const string ValidationFailed = "integrations.validationFailed";
    public const string CannotDeactivateDefault = "integrations.cannotDeactivateDefault";
    public const string CannotSetInactiveDefault = "integrations.cannotSetInactiveDefault";
    public const string Concurrency = "integrations.concurrency";
    public const string TemplateNotFound = "integrations.templateNotFound";
    public const string TemplateVersionNotFound = "integrations.templateVersionNotFound";
    public const string TemplateVersionAlreadyPublished = "integrations.templateVersionAlreadyPublished";
    public const string TestConnectionFailed = "integrations.testConnectionFailed";
}

public static class IntegrationAuditActions
{
    public const string IntegrationCreated = "integrations.created";
    public const string IntegrationUpdated = "integrations.updated";
    public const string IntegrationActivated = "integrations.activated";
    public const string IntegrationDeactivated = "integrations.deactivated";
    public const string IntegrationSetDefault = "integrations.set_default";
    public const string IntegrationValidated = "integrations.validated";
    public const string IntegrationTested = "integrations.tested";
    public const string TemplateVersionCreated = "integrations.template_version_created";
    public const string TemplateVersionPublished = "integrations.template_version_published";
}
