using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ChangeEvaluationTemplateState;

public sealed class ChangeEvaluationTemplateStateCommandValidator
    : AbstractValidator<ChangeEvaluationTemplateStateCommand>
{
    public ChangeEvaluationTemplateStateCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Body.RowVersion).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
