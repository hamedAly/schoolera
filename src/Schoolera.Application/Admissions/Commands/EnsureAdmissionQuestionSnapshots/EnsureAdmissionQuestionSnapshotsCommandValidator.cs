using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.EnsureAdmissionQuestionSnapshots;

public sealed class EnsureAdmissionQuestionSnapshotsCommandValidator
    : AbstractValidator<EnsureAdmissionQuestionSnapshotsCommand>
{
    public EnsureAdmissionQuestionSnapshotsCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
}
