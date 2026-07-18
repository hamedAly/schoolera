using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolPublishedDiscount;

public sealed class CreateSchoolPublishedDiscountCommandValidator
    : AbstractValidator<CreateSchoolPublishedDiscountCommand>
{
    public CreateSchoolPublishedDiscountCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.Body.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Body.EligibilityDescriptionAr).NotEmpty().MaximumLength(1000);
        RuleFor(command => command.Body.Value).GreaterThan(0);
    }
}
