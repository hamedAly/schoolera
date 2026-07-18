using FluentValidation;

namespace Schoolera.Application.Schools.Queries.GetSchools;

public sealed class GetSchoolsQueryValidator : AbstractValidator<GetSchoolsQuery>
{
    public GetSchoolsQueryValidator()
    {
    }
}
