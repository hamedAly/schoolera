using FluentValidation;

namespace Schoolera.Application.Admissions.Queries.GetAdminAdmissionApplication;

public sealed class GetAdminAdmissionApplicationQueryValidator
    : AbstractValidator<GetAdminAdmissionApplicationQuery>
{
    public GetAdminAdmissionApplicationQueryValidator()
    {
        RuleFor(query => query.ApplicationId).NotEmpty();
    }
}
