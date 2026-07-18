using FluentValidation;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.CreateCmsPage;

public sealed class CreateCmsPageCommandValidator : AbstractValidator<CreateCmsPageCommand>
{
    public CreateCmsPageCommandValidator()
    {
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Slug)
            .Must(CmsSlugValidator.IsValidCustomSlug)
            .WithErrorCode(CmsErrorCodes.PageSlugInvalid);

        RuleFor(command => command.TitleAr).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.TitleEn).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.ContentAr).NotEmpty().MaximumLength(FieldLengthLimits.CmsContent);
        RuleFor(command => command.ContentEn).NotEmpty().MaximumLength(FieldLengthLimits.CmsContent);
        RuleFor(command => command.MetaTitleAr).MaximumLength(FieldLengthLimits.CmsMetaTitle);
        RuleFor(command => command.MetaTitleEn).MaximumLength(FieldLengthLimits.CmsMetaTitle);
        RuleFor(command => command.MetaDescriptionAr).MaximumLength(FieldLengthLimits.CmsMetaDescription);
        RuleFor(command => command.MetaDescriptionEn).MaximumLength(FieldLengthLimits.CmsMetaDescription);
    }
}
