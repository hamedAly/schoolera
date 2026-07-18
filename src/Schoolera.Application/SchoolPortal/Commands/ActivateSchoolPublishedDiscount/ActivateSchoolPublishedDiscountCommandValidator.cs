using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolPublishedDiscount;

public sealed class ActivateSchoolPublishedDiscountCommandValidator : AbstractValidator<ActivateSchoolPublishedDiscountCommand>
{
    public ActivateSchoolPublishedDiscountCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
