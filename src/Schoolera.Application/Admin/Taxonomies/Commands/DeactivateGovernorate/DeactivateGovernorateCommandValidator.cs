using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateGovernorate;

public sealed class DeactivateGovernorateCommandValidator : AbstractValidator<DeactivateGovernorateCommand>
{
    public DeactivateGovernorateCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
