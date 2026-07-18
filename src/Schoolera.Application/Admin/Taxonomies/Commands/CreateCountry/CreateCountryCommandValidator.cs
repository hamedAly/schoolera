using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Taxonomies;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateCountry;

public sealed class CreateCountryCommandValidator : AbstractValidator<CreateCountryCommand>
{
    public CreateCountryCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .Length(FieldLengthLimits.CountryCode);
        TaxonomyValidationRules.TaxonomyNameAr(RuleFor(command => command.NameAr), localizer);
        TaxonomyValidationRules.TaxonomyNameEn(RuleFor(command => command.NameEn));
        TaxonomyValidationRules.TaxonomySlug(RuleFor(command => command.Slug));
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}
