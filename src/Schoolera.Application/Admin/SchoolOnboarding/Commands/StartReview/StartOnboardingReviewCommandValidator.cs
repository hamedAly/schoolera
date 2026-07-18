using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.SchoolOnboarding.Commands.StartReview;

public sealed class StartOnboardingReviewCommandValidator
    : AbstractValidator<StartOnboardingReviewCommand>
{
    public StartOnboardingReviewCommandValidator(IStringLocalizer<OnboardingMessages> localizer)
    {
        RuleFor(command => command.ApplicationId)
            .NotEmpty()
            .WithMessage(localizer["NotFound"]);
    }
}
