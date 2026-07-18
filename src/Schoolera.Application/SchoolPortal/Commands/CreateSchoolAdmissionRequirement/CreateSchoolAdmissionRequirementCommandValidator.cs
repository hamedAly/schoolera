using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolAdmissionRequirement;

public sealed class CreateSchoolAdmissionRequirementCommandValidator
    : AbstractValidator<CreateSchoolAdmissionRequirementCommand>
{
    public CreateSchoolAdmissionRequirementCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.RequirementCode).NotEmpty().MaximumLength(64)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(x => x.Body.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body.DescriptionAr).MaximumLength(2000);
        RuleFor(x => x.Body.DescriptionEn).MaximumLength(2000);
        RuleFor(x => x.Body.Kind).IsInEnum();
        RuleFor(x => x.Body.SortOrder).GreaterThanOrEqualTo(0);
    }
}
