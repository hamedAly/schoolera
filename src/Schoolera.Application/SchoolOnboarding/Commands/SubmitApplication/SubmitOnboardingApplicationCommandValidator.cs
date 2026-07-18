using FluentValidation;

namespace Schoolera.Application.SchoolOnboarding.Commands.SubmitApplication;

public sealed class SubmitOnboardingApplicationCommandValidator
    : AbstractValidator<SubmitOnboardingApplicationCommand>
{
    public SubmitOnboardingApplicationCommandValidator()
    {
    }
}
