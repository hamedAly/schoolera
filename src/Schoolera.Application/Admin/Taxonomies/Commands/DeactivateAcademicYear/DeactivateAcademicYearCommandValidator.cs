using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateAcademicYear;

public sealed class DeactivateAcademicYearCommandValidator : AbstractValidator<DeactivateAcademicYearCommand>
{
    public DeactivateAcademicYearCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
