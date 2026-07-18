using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.ResolveContactRequest;

public sealed class ResolveContactRequestCommandValidator : AbstractValidator<ResolveContactRequestCommand>
{
    public ResolveContactRequestCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.AdminNote).MaximumLength(FieldLengthLimits.ContactAdminNote);
    }
}
