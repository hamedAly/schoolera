using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Queries.GetSchoolMeetingSession;

public sealed class GetSchoolMeetingSessionQueryValidator
    : AbstractValidator<GetSchoolMeetingSessionQuery>
{
    public GetSchoolMeetingSessionQueryValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
    }
}
