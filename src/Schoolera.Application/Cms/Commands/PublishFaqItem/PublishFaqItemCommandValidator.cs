using FluentValidation;

namespace Schoolera.Application.Cms.Commands.PublishFaqItem;

public sealed class PublishFaqItemCommandValidator : AbstractValidator<PublishFaqItemCommand>
{
    public PublishFaqItemCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
