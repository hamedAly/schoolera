using FluentValidation;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.RejectSchoolAdmissionApplication;

public sealed class RejectSchoolAdmissionApplicationCommandValidator
    : AbstractValidator<RejectSchoolAdmissionApplicationCommand>
{
    public const int ParentVisibleRejectionReasonMaxLength = 1000;

    public RejectSchoolAdmissionApplicationCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.ParentVisibleRejectionReason)
            .NotEmpty()
            .WithErrorCode(AdmissionErrorCodes.ReviewRejectionReasonRequired)
            .WithMessage("A parent-visible rejection reason is required.")
            .MaximumLength(ParentVisibleRejectionReasonMaxLength);
        RuleFor(command => command.Body.InternalReviewNote)
            .MaximumLength(FieldLengthLimits.AdmissionSchoolNotes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.InternalReviewNote));
    }
}
