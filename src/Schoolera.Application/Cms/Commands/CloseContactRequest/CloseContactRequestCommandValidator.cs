using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.CloseContactRequest;

public sealed class CloseContactRequestCommandValidator : AbstractValidator<CloseContactRequestCommand>
{
    public CloseContactRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.AdminNote).MaximumLength(FieldLengthLimits.ContactAdminNote);
    }
}
