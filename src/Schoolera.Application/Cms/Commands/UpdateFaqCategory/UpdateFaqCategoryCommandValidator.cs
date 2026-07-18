using FluentValidation;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.UpdateFaqCategory;

public sealed class UpdateFaqCategoryCommandValidator : AbstractValidator<UpdateFaqCategoryCommand>
{
    public UpdateFaqCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.NameAr).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.NameEn).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Slug)
            .Must(CmsSlugValidator.IsValidCustomSlug)
            .WithErrorCode(CmsErrorCodes.PageSlugInvalid);
    }
}
