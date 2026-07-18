using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolPublishedDiscount;

public sealed class UpdateSchoolPublishedDiscountCommandValidator
    : AbstractValidator<UpdateSchoolPublishedDiscountCommand>
{
    public UpdateSchoolPublishedDiscountCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.DiscountId).NotEmpty();
        RuleFor(command => command.Body.TitleAr).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Body.EligibilityDescriptionAr).NotEmpty().MaximumLength(1000);
        RuleFor(command => command.Body.Value).GreaterThan(0);
    }
}
