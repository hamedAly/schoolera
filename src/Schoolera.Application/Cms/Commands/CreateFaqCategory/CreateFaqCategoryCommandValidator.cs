using FluentValidation;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.CreateFaqCategory;

public sealed class CreateFaqCategoryCommandValidator : AbstractValidator<CreateFaqCategoryCommand>
{
    public CreateFaqCategoryCommandValidator()
    {
        RuleFor(command => command.NameAr).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.NameEn).NotEmpty().MaximumLength(FieldLengthLimits.CmsTitle);
        RuleFor(command => command.Slug)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Slug)
            .Must(CmsSlugValidator.IsValidCustomSlug)
            .WithErrorCode(CmsErrorCodes.PageSlugInvalid);
    }
}
