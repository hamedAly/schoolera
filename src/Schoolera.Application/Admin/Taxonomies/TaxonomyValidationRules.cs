using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies;

internal static class TaxonomyValidationRules
{
    public static IRuleBuilderOptions<T, string> TaxonomySlug<T>(IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Slug)
            .Must(SlugHelper.IsValidSlug);

    public static IRuleBuilderOptions<T, string> TaxonomyNameAr<T>(
        IRuleBuilder<T, string> ruleBuilder,
        IStringLocalizer<ValidationMessages> localizer) =>
        ruleBuilder
            .NotEmpty()
            .WithMessage(_ => localizer["Required"].Value)
            .MaximumLength(FieldLengthLimits.TaxonomyName)
            .WithMessage(_ => localizer["Required"].Value);

    public static IRuleBuilderOptions<T, string?> TaxonomyNameEn<T>(
        IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.MaximumLength(FieldLengthLimits.TaxonomyName);
}
