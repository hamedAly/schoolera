using FluentValidation;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateDistrict;

public sealed class DeactivateDistrictCommandValidator : AbstractValidator<DeactivateDistrictCommand>
{
    public DeactivateDistrictCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
