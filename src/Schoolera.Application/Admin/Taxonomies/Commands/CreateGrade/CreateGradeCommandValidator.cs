using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Taxonomies;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateGrade;

public sealed class CreateGradeCommandValidator : AbstractValidator<CreateGradeCommand>
{
    public CreateGradeCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.EducationalStageId).NotEmpty();
        TaxonomyValidationRules.TaxonomyNameAr(RuleFor(command => command.NameAr), localizer);
        TaxonomyValidationRules.TaxonomyNameEn(RuleFor(command => command.NameEn));
        TaxonomyValidationRules.TaxonomySlug(RuleFor(command => command.Slug));
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}
