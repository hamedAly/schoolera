using FluentValidation;



namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolAdmissionRequirement;



public sealed class DeactivateSchoolAdmissionRequirementCommandValidator

    : AbstractValidator<DeactivateSchoolAdmissionRequirementCommand>

{

    public DeactivateSchoolAdmissionRequirementCommandValidator()

    {

        RuleFor(x => x.SchoolId).NotEmpty();

        RuleFor(x => x.RequirementId).NotEmpty();

    }

}


