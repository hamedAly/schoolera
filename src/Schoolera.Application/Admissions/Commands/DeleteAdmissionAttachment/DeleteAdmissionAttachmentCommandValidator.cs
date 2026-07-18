using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.DeleteAdmissionAttachment;

public sealed class DeleteAdmissionAttachmentCommandValidator
    : AbstractValidator<DeleteAdmissionAttachmentCommand>
{
    public DeleteAdmissionAttachmentCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.AttachmentId).NotEmpty();
    }
}
