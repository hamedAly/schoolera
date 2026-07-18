using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Taxonomies;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateFacility;

public sealed class CreateFacilityCommandValidator : AbstractValidator<CreateFacilityCommand>
{
    public CreateFacilityCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        TaxonomyValidationRules.TaxonomyNameAr(RuleFor(command => command.NameAr), localizer);
        TaxonomyValidationRules.TaxonomyNameEn(RuleFor(command => command.NameEn));
        TaxonomyValidationRules.TaxonomySlug(RuleFor(command => command.Slug));
        RuleFor(command => command.IconKey)
            .MaximumLength(FieldLengthLimits.IconKey)
            .When(command => !string.IsNullOrWhiteSpace(command.IconKey));
        RuleFor(command => command.SortOrder).GreaterThanOrEqualTo(0);
    }
}
