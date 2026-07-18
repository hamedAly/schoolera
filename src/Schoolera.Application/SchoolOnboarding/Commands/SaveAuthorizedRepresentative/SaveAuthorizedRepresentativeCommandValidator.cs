using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolOnboarding.Commands.SaveAuthorizedRepresentative;

public sealed class SaveAuthorizedRepresentativeCommandValidator
    : AbstractValidator<SaveAuthorizedRepresentativeCommand>
{
    public SaveAuthorizedRepresentativeCommandValidator(
        IStringLocalizer<OnboardingMessages> localizer,
        IStringLocalizer<ValidationMessages> validation)
    {
        RuleFor(command => command.FullNameAr)
            .MaximumLength(FieldLengthLimits.PersonName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.FullNameEn)
            .MaximumLength(FieldLengthLimits.PersonName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.NationalOrIdentityReference)
            .MaximumLength(FieldLengthLimits.IdentityReference)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.JobTitleAr)
            .MaximumLength(FieldLengthLimits.JobTitle)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.JobTitleEn)
            .MaximumLength(FieldLengthLimits.JobTitle)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Email)
            .Must(OnboardingValidationRules.BeAValidOptionalEmail)
            .WithMessage(validation["InvalidEmail"]);

        RuleFor(command => command.Phone)
            .Must(OnboardingValidationRules.BeAValidOptionalPhone)
            .WithMessage(localizer["InvalidPhone"]);
    }
}
