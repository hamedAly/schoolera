using FluentValidation;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.UpdateHomepageContent;

public sealed class UpdateHomepageContentCommandValidator : AbstractValidator<UpdateHomepageContentCommand>
{
    public UpdateHomepageContentCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();

        RuleFor(command => command.Body.HeroTitleAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageHeroTitle);
        RuleFor(command => command.Body.HeroTitleEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageHeroTitle);
        RuleFor(command => command.Body.HeroSubtitleAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageHeroSubtitle);
        RuleFor(command => command.Body.HeroSubtitleEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageHeroSubtitle);
        RuleFor(command => command.Body.PrimaryCtaLabelAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageCtaLabel);
        RuleFor(command => command.Body.PrimaryCtaLabelEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageCtaLabel);
        RuleFor(command => command.Body.PrimaryCtaUrl)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Url)
            .Must(CmsUrlValidator.IsValidCtaUrl)
            .WithErrorCode(CmsErrorCodes.HomeInvalidCtaUrl);
        RuleFor(command => command.Body.SecondaryCtaLabelAr).MaximumLength(FieldLengthLimits.HomepageCtaLabel);
        RuleFor(command => command.Body.SecondaryCtaLabelEn).MaximumLength(FieldLengthLimits.HomepageCtaLabel);
        RuleFor(command => command.Body.SecondaryCtaUrl)
            .MaximumLength(FieldLengthLimits.Url)
            .Must(url => string.IsNullOrWhiteSpace(url) || CmsUrlValidator.IsValidCtaUrl(url))
            .WithErrorCode(CmsErrorCodes.HomeInvalidCtaUrl);
        RuleFor(command => command.Body.SchoolsSectionTitleAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.SchoolsSectionTitleEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.ParentJourneyTitleAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.ParentJourneyTitleEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.ParentJourneyTextAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionText);
        RuleFor(command => command.Body.ParentJourneyTextEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionText);
        RuleFor(command => command.Body.SchoolJourneyTitleAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.SchoolJourneyTitleEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.SchoolJourneyTextAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionText);
        RuleFor(command => command.Body.SchoolJourneyTextEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionText);
        RuleFor(command => command.Body.FaqSectionTitleAr).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.FaqSectionTitleEn).NotEmpty().MaximumLength(FieldLengthLimits.HomepageSectionTitle);
        RuleFor(command => command.Body.FaqSectionSubtitleAr).MaximumLength(FieldLengthLimits.HomepageHeroSubtitle);
        RuleFor(command => command.Body.FaqSectionSubtitleEn).MaximumLength(FieldLengthLimits.HomepageHeroSubtitle);
    }
}
