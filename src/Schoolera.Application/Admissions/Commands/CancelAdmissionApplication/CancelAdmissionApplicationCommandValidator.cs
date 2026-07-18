using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.CancelAdmissionApplication;

public sealed class CancelAdmissionApplicationCommandValidator
    : AbstractValidator<CancelAdmissionApplicationCommand>
{
    public CancelAdmissionApplicationCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Reason)
            .MaximumLength(FieldLengthLimits.AdmissionCancellationReason)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Reason));
    }
}
