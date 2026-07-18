using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Payments.Common;

public interface IPaymentProviderResolver
{
    IPaymentProvider? ResolvePayment(string providerCode);

    IFinancingProvider? ResolveFinancing(string providerCode);

    Task<PaymentIntegrationSettingsContext?> ResolveActivePaymentIntegrationAsync(
        Guid? integrationConfigurationId,
        CancellationToken cancellationToken = default);

    Task<FinancingIntegrationSettingsContext?> ResolveActiveFinancingIntegrationAsync(
        Guid? integrationConfigurationId,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentProviderResolver(
    IEnumerable<IPaymentProvider> paymentProviders,
    IEnumerable<IFinancingProvider> financingProviders,
    INotificationRepository notificationRepository,
    IPlatformIntegrationConfigurationAccessor configurationAccessor) : IPaymentProviderResolver
{
    public IPaymentProvider? ResolvePayment(string providerCode) =>
        paymentProviders.FirstOrDefault(provider =>
            string.Equals(provider.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase));

    public IFinancingProvider? ResolveFinancing(string providerCode) =>
        financingProviders.FirstOrDefault(provider =>
            string.Equals(provider.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase));

    public async Task<PaymentIntegrationSettingsContext?> ResolveActivePaymentIntegrationAsync(
        Guid? integrationConfigurationId,
        CancellationToken cancellationToken = default)
    {
        Domain.Entities.PlatformIntegrationConfiguration? entity = null;
        if (integrationConfigurationId is { } id)
        {
            entity = await notificationRepository.GetIntegrationAsync(id, cancellationToken);
            if (entity is null ||
                entity.IntegrationType != IntegrationType.Payment ||
                !entity.IsActive)
            {
                return null;
            }
        }
        else
        {
            var resolved = await configurationAccessor.GetActiveDefaultAsync(
                IntegrationType.Payment, cancellationToken);
            if (resolved is null || !resolved.IsConfigured)
            {
                return null;
            }

            entity = await notificationRepository.GetIntegrationAsync(resolved.Id, cancellationToken);
            if (entity is null || !entity.IsActive)
            {
                return null;
            }
        }

        var settings = IntegrationSettingsSerializer.Deserialize<PaymentIntegrationSettings>(entity.SettingsJson);
        return new PaymentIntegrationSettingsContext(
            entity.Id,
            entity.ProviderCode,
            entity.DisplayNameAr,
            entity.DisplayNameEn,
            PaymentMapping.ParseEnvironment(settings.Environment),
            entity.SettingsJson);
    }

    public async Task<FinancingIntegrationSettingsContext?> ResolveActiveFinancingIntegrationAsync(
        Guid? integrationConfigurationId,
        CancellationToken cancellationToken = default)
    {
        Domain.Entities.PlatformIntegrationConfiguration? entity = null;
        if (integrationConfigurationId is { } id)
        {
            entity = await notificationRepository.GetIntegrationAsync(id, cancellationToken);
            if (entity is null ||
                entity.IntegrationType != IntegrationType.Financing ||
                !entity.IsActive)
            {
                return null;
            }
        }
        else
        {
            var resolved = await configurationAccessor.GetActiveDefaultAsync(
                IntegrationType.Financing, cancellationToken);
            if (resolved is null || !resolved.IsConfigured)
            {
                return null;
            }

            entity = await notificationRepository.GetIntegrationAsync(resolved.Id, cancellationToken);
            if (entity is null || !entity.IsActive)
            {
                return null;
            }
        }

        var settings = IntegrationSettingsSerializer.Deserialize<FinancingIntegrationSettings>(entity.SettingsJson);
        return new FinancingIntegrationSettingsContext(
            entity.Id,
            entity.ProviderCode,
            entity.DisplayNameAr,
            entity.DisplayNameEn,
            PaymentMapping.ParseEnvironment(settings.Environment),
            entity.SettingsJson);
    }
}
