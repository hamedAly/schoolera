using FluentValidation;

namespace Schoolera.Application.Cms.Commands.PublishHomepageContent;

public sealed class PublishHomepageContentCommandValidator : AbstractValidator<PublishHomepageContentCommand>
{
    public PublishHomepageContentCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
