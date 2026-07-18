using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Taxonomies;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateAcademicYear;

public sealed class UpdateAcademicYearCommandValidator : AbstractValidator<UpdateAcademicYearCommand>
{
    public UpdateAcademicYearCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Id).NotEmpty();
        TaxonomyValidationRules.TaxonomyNameAr(RuleFor(command => command.NameAr), localizer);
        TaxonomyValidationRules.TaxonomyNameEn(RuleFor(command => command.NameEn));
        TaxonomyValidationRules.TaxonomySlug(RuleFor(command => command.Slug));
        RuleFor(command => command)
            .Must(command => command.EndDate > command.StartDate)
            .WithMessage(_ => localizer["Required"].Value);
    }
}
