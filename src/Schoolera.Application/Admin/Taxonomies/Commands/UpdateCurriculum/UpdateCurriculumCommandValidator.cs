using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Taxonomies;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateCurriculum;

public sealed class UpdateCurriculumCommandValidator : AbstractValidator<UpdateCurriculumCommand>
{
    public UpdateCurriculumCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Id).NotEmpty();
        TaxonomyValidationRules.TaxonomyNameAr(RuleFor(command => command.NameAr), localizer);
        TaxonomyValidationRules.TaxonomyNameEn(RuleFor(command => command.NameEn));
        TaxonomyValidationRules.TaxonomySlug(RuleFor(command => command.Slug));
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}
