using FluentValidation;

namespace Schoolera.Application.Admissions.Queries.GetSchoolAdmissionApplication;

public sealed class GetSchoolAdmissionApplicationQueryValidator
    : AbstractValidator<GetSchoolAdmissionApplicationQuery>
{
    public GetSchoolAdmissionApplicationQueryValidator()
    {
        RuleFor(query => query.SchoolId).NotEmpty();
        RuleFor(query => query.ApplicationId).NotEmpty();
    }
}
