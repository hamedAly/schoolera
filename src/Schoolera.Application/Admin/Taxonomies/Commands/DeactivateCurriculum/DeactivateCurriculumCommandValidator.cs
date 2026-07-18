using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCurriculum;

public sealed class DeactivateCurriculumCommandValidator : AbstractValidator<DeactivateCurriculumCommand>
{
    public DeactivateCurriculumCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
