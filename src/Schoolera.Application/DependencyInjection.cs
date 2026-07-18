using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Common.Behaviors;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Integrations;
using Schoolera.Application.Payments.Common;
using Schoolera.Application.Payments.Providers;

namespace Schoolera.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddScoped<IAdmissionEligibilityService, AdmissionEligibilityService>();
        services.AddScoped<IChildAgeEligibilityEvaluator, ChildAgeEligibilityEvaluator>();
        services.AddSingleton<IIntegrationSettingsValidator, IntegrationSettingsValidator>();
        services.AddScoped<IPaymentProvider, SchooleraSandboxPaymentProvider>();
        services.AddScoped<IFinancingProvider, SchooleraSandboxFinancingProvider>();
        services.AddScoped<IPayableAmountResolver, PayableAmountResolver>();
        services.AddScoped<IPaymentProviderResolver, PaymentProviderResolver>();

        return services;
    }
}
