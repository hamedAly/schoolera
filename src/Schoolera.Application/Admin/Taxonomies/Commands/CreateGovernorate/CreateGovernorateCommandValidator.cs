using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Taxonomies;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateGovernorate;

public sealed class CreateGovernorateCommandValidator : AbstractValidator<CreateGovernorateCommand>
{
    public CreateGovernorateCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.CountryId).NotEmpty();
        TaxonomyValidationRules.TaxonomyNameAr(RuleFor(command => command.NameAr), localizer);
        TaxonomyValidationRules.TaxonomyNameEn(RuleFor(command => command.NameEn));
        TaxonomyValidationRules.TaxonomySlug(RuleFor(command => command.Slug));
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}
