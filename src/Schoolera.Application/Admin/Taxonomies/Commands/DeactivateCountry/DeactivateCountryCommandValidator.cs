using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateCountry;

public sealed class DeactivateCountryCommandValidator : AbstractValidator<DeactivateCountryCommand>
{
    public DeactivateCountryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
