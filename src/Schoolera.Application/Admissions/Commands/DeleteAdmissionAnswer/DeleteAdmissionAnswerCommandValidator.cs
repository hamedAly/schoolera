using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.DeleteAdmissionAnswer;

public sealed class DeleteAdmissionAnswerCommandValidator
    : AbstractValidator<DeleteAdmissionAnswerCommand>
{
    public DeleteAdmissionAnswerCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.QuestionSnapshotId).NotEmpty();
    }
}
