using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolOnboarding.Commands.SaveSchoolDetails;

public sealed class SaveSchoolDetailsCommandValidator : AbstractValidator<SaveSchoolDetailsCommand>
{
    public SaveSchoolDetailsCommandValidator(IStringLocalizer<OnboardingMessages> localizer)
    {
        RuleFor(command => command.SchoolNameAr)
            .MaximumLength(FieldLengthLimits.SchoolName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.SchoolNameEn)
            .MaximumLength(FieldLengthLimits.SchoolName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.SchoolType)
            .IsInEnum()
            .When(command => command.SchoolType.HasValue)
            .WithMessage(localizer["Incomplete"]);

        RuleFor(command => command.GenderType)
            .IsInEnum()
            .When(command => command.GenderType.HasValue)
            .WithMessage(localizer["Incomplete"]);

        RuleFor(command => command.FoundedYear)
            .Must(year => year is null || (year >= 1800 && year <= DateTimeOffset.UtcNow.Year))
            .WithMessage(localizer["InvalidFoundedYear"]);

        RuleFor(command => command.ShortDescriptionAr)
            .MaximumLength(FieldLengthLimits.TaxonomyDescription)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.ShortDescriptionEn)
            .MaximumLength(FieldLengthLimits.TaxonomyDescription)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.WebsiteUrl)
            .Must(OnboardingValidationRules.BeAValidOptionalUrl)
            .WithMessage(localizer["InvalidUrl"]);

        RuleFor(command => command.RequestedSlug)
            .MaximumLength(FieldLengthLimits.Slug)
            .WithMessage(localizer["FieldMaxLength"]);
    }
}
