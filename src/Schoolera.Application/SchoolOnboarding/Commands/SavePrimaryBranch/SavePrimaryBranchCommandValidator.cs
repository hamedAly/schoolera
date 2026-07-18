using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolOnboarding.Commands.SavePrimaryBranch;

public sealed class SavePrimaryBranchCommandValidator : AbstractValidator<SavePrimaryBranchCommand>
{
    public SavePrimaryBranchCommandValidator(
        IStringLocalizer<OnboardingMessages> localizer,
        IStringLocalizer<ValidationMessages> validation)
    {
        RuleFor(command => command.AddressLineAr)
            .MaximumLength(FieldLengthLimits.AddressLine)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.AddressLineEn)
            .MaximumLength(FieldLengthLimits.AddressLine)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.BuildingNumber)
            .MaximumLength(FieldLengthLimits.BranchCode)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.StreetName)
            .MaximumLength(FieldLengthLimits.AddressLine)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Landmark)
            .MaximumLength(FieldLengthLimits.Landmark)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.PostalCode)
            .MaximumLength(FieldLengthLimits.PostalCode)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.LocalAddressReference)
            .MaximumLength(FieldLengthLimits.AddressLine)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Latitude)
            .InclusiveBetween(-90m, 90m)
            .When(command => command.Latitude.HasValue)
            .WithMessage(localizer["InvalidCoordinates"]);

        RuleFor(command => command.Longitude)
            .InclusiveBetween(-180m, 180m)
            .When(command => command.Longitude.HasValue)
            .WithMessage(localizer["InvalidCoordinates"]);

        RuleFor(command => command.PublicEmail)
            .Must(OnboardingValidationRules.BeAValidOptionalEmail)
            .WithMessage(validation["InvalidEmail"]);

        RuleFor(command => command.PublicPhone)
            .Must(OnboardingValidationRules.BeAValidOptionalPhone)
            .WithMessage(localizer["InvalidPhone"]);

        RuleFor(command => command.WhatsAppOrAlternatePhone)
            .Must(OnboardingValidationRules.BeAValidOptionalPhone)
            .WithMessage(localizer["InvalidPhone"]);
    }
}
