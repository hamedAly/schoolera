using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolOnboarding.Commands.SaveOrganization;

public sealed class SaveOrganizationCommandValidator : AbstractValidator<SaveOrganizationCommand>
{
    public SaveOrganizationCommandValidator(IStringLocalizer<OnboardingMessages> localizer)
    {
        RuleFor(command => command.OrganizationNameAr)
            .MaximumLength(FieldLengthLimits.OrganizationName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.OrganizationNameEn)
            .MaximumLength(FieldLengthLimits.OrganizationName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.LegalName)
            .MaximumLength(FieldLengthLimits.LegalName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.CountryCode)
            .Must(OnboardingValidationRules.BeAValidOptionalCountryCode)
            .WithMessage(localizer["InvalidCountryCode"]);

        RuleFor(command => command.RegistrationOrLicenseNumber)
            .MaximumLength(FieldLengthLimits.RegistrationNumber)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.TaxRegistrationNumber)
            .MaximumLength(FieldLengthLimits.TaxNumber)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.LegalForm)
            .MaximumLength(FieldLengthLimits.LegalForm)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.OrganizationAddress)
            .MaximumLength(FieldLengthLimits.AddressLine)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.OrganizationWebsite)
            .Must(OnboardingValidationRules.BeAValidOptionalUrl)
            .WithMessage(localizer["InvalidUrl"]);
    }
}
