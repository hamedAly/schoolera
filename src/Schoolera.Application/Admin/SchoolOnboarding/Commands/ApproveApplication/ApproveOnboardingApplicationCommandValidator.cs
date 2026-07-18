using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admin.SchoolOnboarding.Commands.ApproveApplication;

public sealed class ApproveOnboardingApplicationCommandValidator
    : AbstractValidator<ApproveOnboardingApplicationCommand>
{
    public ApproveOnboardingApplicationCommandValidator(IStringLocalizer<OnboardingMessages> localizer)
    {
        RuleFor(command => command.ApplicationId)
            .NotEmpty()
            .WithMessage(localizer["NotFound"]);

        RuleFor(command => command.InternalNote)
            .MaximumLength(FieldLengthLimits.OnboardingReason)
            .WithMessage(localizer["ReasonTooLong"]);
    }
}
