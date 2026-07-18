using FluentValidation;

namespace Schoolera.Application.Integrations.Commands.RetryMeetingSession;

public sealed class RetryMeetingSessionCommandValidator
    : AbstractValidator<RetryMeetingSessionCommand>
{
    public RetryMeetingSessionCommandValidator()
    {
        RuleFor(x => x.MeetingSessionId).NotEmpty();
        RuleFor(x => x.Body.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Body.RowVersion).NotEmpty();
    }
}
