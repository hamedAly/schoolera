using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateGrade;

public sealed class DeactivateGradeCommandValidator : AbstractValidator<DeactivateGradeCommand>
{
    public DeactivateGradeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
