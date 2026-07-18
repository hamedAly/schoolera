using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCity;

public sealed class DeactivateCityCommandValidator : AbstractValidator<DeactivateCityCommand>
{
    public DeactivateCityCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
