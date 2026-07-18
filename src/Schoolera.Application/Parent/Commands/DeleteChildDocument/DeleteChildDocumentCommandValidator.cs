using FluentValidation;

namespace Schoolera.Application.Parent.Commands.DeleteChildDocument;

public sealed class DeleteChildDocumentCommandValidator : AbstractValidator<DeleteChildDocumentCommand>
{
    public DeleteChildDocumentCommandValidator()
    {
        RuleFor(command => command.ChildId).NotEmpty();
        RuleFor(command => command.DocumentId).NotEmpty();
    }
}
