using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.CreateAdmissionApplication;

public sealed class CreateAdmissionApplicationCommandValidator
    : AbstractValidator<CreateAdmissionApplicationCommand>
{
    public CreateAdmissionApplicationCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.ChildProfileId).NotEmpty();
        RuleFor(command => command.Body.SchoolBranchId).NotEmpty();
        RuleFor(command => command.Body.EducationalStageId).NotEmpty();
        RuleFor(command => command.Body.GradeId).NotEmpty();
        RuleFor(command => command.Body.AcademicYearId).NotEmpty();
        RuleFor(command => command.Body)
            .Must(body => body.SchoolId is not null || !string.IsNullOrWhiteSpace(body.SchoolSlug))
            .WithMessage("SchoolId or SchoolSlug is required.");
        RuleFor(command => command.Body.SchoolSlug)
            .MaximumLength(FieldLengthLimits.Slug)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.SchoolSlug));
        RuleFor(command => command.Body.ParentNotes)
            .MaximumLength(FieldLengthLimits.AdmissionParentNotes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.ParentNotes));
    }
}
