using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.AcceptSchoolAdmissionApplication;

public sealed class AcceptSchoolAdmissionApplicationCommandValidator
    : AbstractValidator<AcceptSchoolAdmissionApplicationCommand>
{
    public AcceptSchoolAdmissionApplicationCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.InternalReviewNote)
            .MaximumLength(FieldLengthLimits.AdmissionSchoolNotes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.InternalReviewNote));
    }
}
