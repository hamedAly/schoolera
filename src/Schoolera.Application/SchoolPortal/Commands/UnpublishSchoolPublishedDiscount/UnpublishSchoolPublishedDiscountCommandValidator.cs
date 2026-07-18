using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolPublishedDiscount;

public sealed class UnpublishSchoolPublishedDiscountCommandValidator : AbstractValidator<UnpublishSchoolPublishedDiscountCommand>
{
    public UnpublishSchoolPublishedDiscountCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
