using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ResolveSchoolMeetingHost;

public sealed class ResolveSchoolMeetingHostCommandValidator
    : AbstractValidator<ResolveSchoolMeetingHostCommand>
{
    public ResolveSchoolMeetingHostCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
    }
}
