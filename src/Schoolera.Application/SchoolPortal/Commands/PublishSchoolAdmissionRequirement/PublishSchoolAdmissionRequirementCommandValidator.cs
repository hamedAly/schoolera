using FluentValidation;



namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolAdmissionRequirement;



public sealed class PublishSchoolAdmissionRequirementCommandValidator

    : AbstractValidator<PublishSchoolAdmissionRequirementCommand>

{

    public PublishSchoolAdmissionRequirementCommandValidator()

    {

        RuleFor(x => x.SchoolId).NotEmpty();

        RuleFor(x => x.RequirementId).NotEmpty();

    }

}


