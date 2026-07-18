using FluentValidation;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.UpdateCmsPage;

public sealed class UpdateCmsPageCommandValidator : AbstractValidator<UpdateCmsPageCommand>
{
    public UpdateCmsPageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Body).NotNull();

        RuleFor(command => command.Body.TitleAr).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.Body.TitleEn).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.Body.ContentAr).NotEmpty().MaximumLength(FieldLengthLimits.CmsContent);
        RuleFor(command => command.Body.ContentEn).NotEmpty().MaximumLength(FieldLengthLimits.CmsContent);
        RuleFor(command => command.Body.MetaTitleAr).MaximumLength(FieldLengthLimits.CmsMetaTitle);
        RuleFor(command => command.Body.MetaTitleEn).MaximumLength(FieldLengthLimits.CmsMetaTitle);
        RuleFor(command => command.Body.MetaDescriptionAr).MaximumLength(FieldLengthLimits.CmsMetaDescription);
        RuleFor(command => command.Body.MetaDescriptionEn).MaximumLength(FieldLengthLimits.CmsMetaDescription);
        RuleFor(command => command.Body.RowVersion).NotEmpty();

        RuleFor(command => command.Body.Slug)
            .MaximumLength(FieldLengthLimits.Slug)
            .Must(slug => string.IsNullOrWhiteSpace(slug) || CmsSlugValidator.IsValidCustomSlug(slug))
            .WithErrorCode(CmsErrorCodes.PageSlugInvalid)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Slug));
    }
}
