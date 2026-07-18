using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateEducationalStage;

public sealed class DeactivateEducationalStageCommandValidator : AbstractValidator<DeactivateEducationalStageCommand>
{
    public DeactivateEducationalStageCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
