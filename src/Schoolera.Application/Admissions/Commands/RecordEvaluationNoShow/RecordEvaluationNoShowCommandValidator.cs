using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.RecordEvaluationNoShow;

public sealed class RecordEvaluationNoShowCommandValidator
    : AbstractValidator<RecordEvaluationNoShowCommand>
{
    public RecordEvaluationNoShowCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Body.AppointmentRowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
