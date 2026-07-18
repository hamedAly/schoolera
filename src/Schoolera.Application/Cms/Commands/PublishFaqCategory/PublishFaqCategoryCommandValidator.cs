using FluentValidation;

namespace Schoolera.Application.Cms.Commands.PublishFaqCategory;

public sealed class PublishFaqCategoryCommandValidator : AbstractValidator<PublishFaqCategoryCommand>
{
    public PublishFaqCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
