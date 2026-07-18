using FluentValidation;

namespace Schoolera.Application.Cms.Commands.UnpublishHomepageContent;

public sealed class UnpublishHomepageContentCommandValidator : AbstractValidator<UnpublishHomepageContentCommand>
{
    public UnpublishHomepageContentCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
