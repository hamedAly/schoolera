using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolPortalProfile;

public sealed class UpdateSchoolPortalProfileCommandValidator : AbstractValidator<UpdateSchoolPortalProfileCommand>
{
    public UpdateSchoolPortalProfileCommandValidator(IStringLocalizer<SchoolPortalMessages> localizer)
    {
        RuleFor(command => command.Body.NameAr)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SchoolName)
            .WithMessage(localizer["RequiredField"]);

        RuleFor(command => command.Body.NameEn)
            .MaximumLength(FieldLengthLimits.SchoolName)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Body.ShortDescriptionAr)
            .MaximumLength(FieldLengthLimits.ShortDescription)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Body.ShortDescriptionEn)
            .MaximumLength(FieldLengthLimits.ShortDescription)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Body.FullDescriptionAr)
            .MaximumLength(FieldLengthLimits.FullDescription)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Body.FullDescriptionEn)
            .MaximumLength(FieldLengthLimits.FullDescription)
            .WithMessage(localizer["FieldMaxLength"]);

        RuleFor(command => command.Body.FoundedYear)
            .Must(SchoolPortalValidationRules.BeAValidFoundedYear)
            .WithMessage(localizer["InvalidFoundedYear"]);

        RuleFor(command => command.Body.PublicEmail)
            .Must(SchoolPortalValidationRules.BeAValidOptionalEmail)
            .WithMessage(localizer["InvalidUrl"]);

        RuleFor(command => command.Body.PublicPhone)
            .Must(SchoolPortalValidationRules.BeAValidOptionalPhone)
            .WithMessage(localizer["InvalidPhone"]);

        RuleFor(command => command.Body.WebsiteUrl)
            .Must(SchoolPortalValidationRules.BeAValidOptionalUrl)
            .WithMessage(localizer["InvalidUrl"]);
    }
}
