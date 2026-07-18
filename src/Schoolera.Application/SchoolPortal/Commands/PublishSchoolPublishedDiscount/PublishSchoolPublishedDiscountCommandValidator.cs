using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolPublishedDiscount;

public sealed class PublishSchoolPublishedDiscountCommandValidator : AbstractValidator<PublishSchoolPublishedDiscountCommand>
{
    public PublishSchoolPublishedDiscountCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
