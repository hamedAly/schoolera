using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.StartEvaluationSession;

public sealed class StartEvaluationSessionCommandValidator
    : AbstractValidator<StartEvaluationSessionCommand>
{
    public StartEvaluationSessionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.AppointmentRowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
