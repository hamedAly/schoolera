using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.UpsertAdmissionAnswer;

public sealed class UpsertAdmissionAnswerCommandValidator
    : AbstractValidator<UpsertAdmissionAnswerCommand>
{
    public UpsertAdmissionAnswerCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.QuestionSnapshotId).NotEmpty();
    }
}
