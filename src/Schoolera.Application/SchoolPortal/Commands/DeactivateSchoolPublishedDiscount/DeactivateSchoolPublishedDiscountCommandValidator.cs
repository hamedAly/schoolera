using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolPublishedDiscount;

public sealed class DeactivateSchoolPublishedDiscountCommandValidator : AbstractValidator<DeactivateSchoolPublishedDiscountCommand>
{
    public DeactivateSchoolPublishedDiscountCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
