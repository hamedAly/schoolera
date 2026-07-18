using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.ScheduleAdmissionInterview;

public sealed class ScheduleAdmissionInterviewCommandValidator
    : AbstractValidator<ScheduleAdmissionInterviewCommand>
{
    public ScheduleAdmissionInterviewCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.InternalReviewNote)
            .MaximumLength(FieldLengthLimits.AdmissionSchoolNotes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.InternalReviewNote));
        RuleFor(command => command.Body.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128)
            .When(command => command.Body.SlotId.HasValue);
    }
}
