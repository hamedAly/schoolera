using FluentValidation;

namespace Schoolera.Application.Cms.Commands.UnpublishFaqItem;

public sealed class UnpublishFaqItemCommandValidator : AbstractValidator<UnpublishFaqItemCommand>
{
    public UnpublishFaqItemCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
