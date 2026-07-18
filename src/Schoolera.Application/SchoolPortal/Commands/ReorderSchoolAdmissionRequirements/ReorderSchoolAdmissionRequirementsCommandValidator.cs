using FluentValidation;



namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolAdmissionRequirements;



public sealed class ReorderSchoolAdmissionRequirementsCommandValidator

    : AbstractValidator<ReorderSchoolAdmissionRequirementsCommand>

{

    public ReorderSchoolAdmissionRequirementsCommandValidator()

    {

        RuleFor(x => x.SchoolId).NotEmpty();

        RuleFor(x => x.Body).NotNull();

        RuleFor(x => x.Body.OrderedRequirementIds).NotNull();

    }

}


