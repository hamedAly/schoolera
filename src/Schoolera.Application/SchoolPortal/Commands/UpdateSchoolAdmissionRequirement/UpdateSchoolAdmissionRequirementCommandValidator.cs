using FluentValidation;



namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolAdmissionRequirement;



public sealed class UpdateSchoolAdmissionRequirementCommandValidator

    : AbstractValidator<UpdateSchoolAdmissionRequirementCommand>

{

    public UpdateSchoolAdmissionRequirementCommandValidator()

    {

        RuleFor(x => x.SchoolId).NotEmpty();

        RuleFor(x => x.RequirementId).NotEmpty();

        RuleFor(x => x.Body).NotNull();

        RuleFor(x => x.Body.NameAr).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Body.NameEn).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Body.DescriptionAr).MaximumLength(2000);

        RuleFor(x => x.Body.DescriptionEn).MaximumLength(2000);

        RuleFor(x => x.Body.SortOrder).GreaterThanOrEqualTo(0);

    }

}


