using FluentValidation;

namespace Schoolera.Application.Cms.Commands.ArchiveCmsPage;

public sealed class ArchiveCmsPageCommandValidator : AbstractValidator<ArchiveCmsPageCommand>
{
    public ArchiveCmsPageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
