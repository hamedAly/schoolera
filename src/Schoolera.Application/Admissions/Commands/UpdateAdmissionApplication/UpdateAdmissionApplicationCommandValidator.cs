using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.UpdateAdmissionApplication;

public sealed class UpdateAdmissionApplicationCommandValidator
    : AbstractValidator<UpdateAdmissionApplicationCommand>
{
    public UpdateAdmissionApplicationCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.SchoolBranchId).NotEmpty();
        RuleFor(command => command.Body.EducationalStageId).NotEmpty();
        RuleFor(command => command.Body.GradeId).NotEmpty();
        RuleFor(command => command.Body.AcademicYearId).NotEmpty();
        RuleFor(command => command.Body.ParentNotes)
            .MaximumLength(FieldLengthLimits.AdmissionParentNotes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.ParentNotes));
    }
}
