using FluentValidation;

namespace Schoolera.Application.Parent.Commands.CopyChildDocumentToAdmissionDraft;

public sealed class CopyChildDocumentToAdmissionDraftCommandValidator
    : AbstractValidator<CopyChildDocumentToAdmissionDraftCommand>
{
    public CopyChildDocumentToAdmissionDraftCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.ChildDocumentId).NotEmpty();
    }
}
