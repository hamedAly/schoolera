using FluentValidation;



namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolAdmissionRequirement;



public sealed class UnpublishSchoolAdmissionRequirementCommandValidator

    : AbstractValidator<UnpublishSchoolAdmissionRequirementCommand>

{

    public UnpublishSchoolAdmissionRequirementCommandValidator()

    {

        RuleFor(x => x.SchoolId).NotEmpty();

        RuleFor(x => x.RequirementId).NotEmpty();

    }

}


