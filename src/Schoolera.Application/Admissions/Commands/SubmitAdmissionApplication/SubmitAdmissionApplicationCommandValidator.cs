using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.SubmitAdmissionApplication;

public sealed class SubmitAdmissionApplicationCommandValidator
    : AbstractValidator<SubmitAdmissionApplicationCommand>
{
    public SubmitAdmissionApplicationCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
    }
}
