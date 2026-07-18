using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.UploadAdmissionAttachment;

public sealed class UploadAdmissionAttachmentCommandValidator
    : AbstractValidator<UploadAdmissionAttachmentCommand>
{
    public UploadAdmissionAttachmentCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
    }
}
