using FluentValidation;

namespace Schoolera.Application.Cms.Commands.PublishCmsPage;

public sealed class PublishCmsPageCommandValidator : AbstractValidator<PublishCmsPageCommand>
{
    public PublishCmsPageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
