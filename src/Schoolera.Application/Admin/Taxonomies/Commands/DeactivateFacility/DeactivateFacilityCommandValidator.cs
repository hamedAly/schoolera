using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateFacility;

public sealed class DeactivateFacilityCommandValidator : AbstractValidator<DeactivateFacilityCommand>
{
    public DeactivateFacilityCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
