using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SupportTickets.Commands.UploadSupportTicketAttachmentAsParent;

public sealed class UploadSupportTicketAttachmentAsParentCommandValidator
    : AbstractValidator<UploadSupportTicketAttachmentAsParentCommand>
{
    public UploadSupportTicketAttachmentAsParentCommandValidator()
    {
        RuleFor(command => command.TicketId).NotEmpty();
        RuleFor(command => command.OriginalFileName)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketFileName);
        RuleFor(command => command.ContentType)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.SupportTicketContentType);
        RuleFor(command => command.FileSize).GreaterThan(0);
        RuleFor(command => command.Content).NotNull();
    }
}
