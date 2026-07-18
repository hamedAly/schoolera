using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.StartContactRequestReview;

public sealed class StartContactRequestReviewCommandValidator : AbstractValidator<StartContactRequestReviewCommand>
{
    public StartContactRequestReviewCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.AdminNote).MaximumLength(FieldLengthLimits.ContactAdminNote);
    }
}
