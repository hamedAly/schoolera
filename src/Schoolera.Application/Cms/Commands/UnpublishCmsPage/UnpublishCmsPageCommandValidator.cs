using FluentValidation;

namespace Schoolera.Application.Cms.Commands.UnpublishCmsPage;

public sealed class UnpublishCmsPageCommandValidator : AbstractValidator<UnpublishCmsPageCommand>
{
    public UnpublishCmsPageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
