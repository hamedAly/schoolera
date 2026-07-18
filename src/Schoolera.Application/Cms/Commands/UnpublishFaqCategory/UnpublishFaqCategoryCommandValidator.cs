using FluentValidation;

namespace Schoolera.Application.Cms.Commands.UnpublishFaqCategory;

public sealed class UnpublishFaqCategoryCommandValidator : AbstractValidator<UnpublishFaqCategoryCommand>
{
    public UnpublishFaqCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
