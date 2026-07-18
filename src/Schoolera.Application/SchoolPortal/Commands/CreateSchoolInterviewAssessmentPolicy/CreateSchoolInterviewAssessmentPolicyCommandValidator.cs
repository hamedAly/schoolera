using FluentValidation;
using Schoolera.Application.Admissions.Common;
using Schoolera.Domain.Common;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolInterviewAssessmentPolicy;

public sealed class CreateSchoolInterviewAssessmentPolicyCommandValidator
    : AbstractValidator<CreateSchoolInterviewAssessmentPolicyCommand>
{
    public CreateSchoolInterviewAssessmentPolicyCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.RequirementMode).IsInEnum();
        RuleFor(x => x.Body.DeliveryMode).IsInEnum().When(x => x.Body.DeliveryMode is not null);
        RuleFor(x => x.Body.RequiredParticipants).IsInEnum().When(x => x.Body.RequiredParticipants is not null);
        RuleFor(x => x.Body.HybridSelectionAuthority).IsInEnum().When(x => x.Body.HybridSelectionAuthority is not null);
        RuleFor(x => x.Body.PreparationNotesAr).MaximumLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        RuleFor(x => x.Body.PreparationNotesEn).MaximumLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        RuleFor(x => x.Body.OnSiteInstructionsAr).MaximumLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        RuleFor(x => x.Body.OnSiteInstructionsEn).MaximumLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        RuleFor(x => x.Body.OnlineInstructionsAr).MaximumLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        RuleFor(x => x.Body.OnlineInstructionsEn).MaximumLength(FieldLengthLimits.InterviewAssessmentPolicyNotes);
        RuleFor(x => x.Body.MeetingProviderCode).MaximumLength(FieldLengthLimits.MeetingProviderCode);
        RuleFor(x => x.Body.MaxParentRescheduleAttempts)
            .InclusiveBetween(
                InterviewAssessmentPolicyCatalog.MinMaxRescheduleAttempts,
                InterviewAssessmentPolicyCatalog.MaxMaxRescheduleAttempts);
        RuleFor(x => x.Body.ExpectedDurationMinutes)
            .InclusiveBetween(
                InterviewAssessmentPolicyCatalog.MinDurationMinutes,
                InterviewAssessmentPolicyCatalog.MaxDurationMinutes)
            .When(x => x.Body.ExpectedDurationMinutes is not null);
        RuleFor(x => x.Body.MinimumSchedulingLeadTimeHours)
            .InclusiveBetween(
                InterviewAssessmentPolicyCatalog.MinLeadTimeHours,
                InterviewAssessmentPolicyCatalog.MaxLeadTimeHours)
            .When(x => x.Body.MinimumSchedulingLeadTimeHours is not null);
        RuleFor(x => x.Body.BookingWindowOpensDaysBefore)
            .InclusiveBetween(
                InterviewAssessmentPolicyCatalog.MinBookingWindowDays,
                InterviewAssessmentPolicyCatalog.MaxBookingWindowDays)
            .When(x => x.Body.BookingWindowOpensDaysBefore is not null);
        RuleFor(x => x.Body.BookingWindowClosesDaysBefore)
            .InclusiveBetween(
                InterviewAssessmentPolicyCatalog.MinBookingWindowDays,
                InterviewAssessmentPolicyCatalog.MaxBookingWindowDays)
            .When(x => x.Body.BookingWindowClosesDaysBefore is not null);
    }
}
