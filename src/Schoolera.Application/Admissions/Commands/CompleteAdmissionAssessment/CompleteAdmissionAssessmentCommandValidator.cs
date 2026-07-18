using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.CompleteAdmissionAssessment;

public sealed class CompleteAdmissionAssessmentCommandValidator
    : AbstractValidator<CompleteAdmissionAssessmentCommand>
{
    public CompleteAdmissionAssessmentCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.InternalReviewNote)
            .MaximumLength(FieldLengthLimits.AdmissionSchoolNotes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.InternalReviewNote));
    }
}
