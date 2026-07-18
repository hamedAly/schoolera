using FluentValidation;

namespace Schoolera.Application.SchoolOnboarding.Commands.ResubmitApplication;

public sealed class ResubmitOnboardingApplicationCommandValidator
    : AbstractValidator<ResubmitOnboardingApplicationCommand>
{
    public ResubmitOnboardingApplicationCommandValidator()
    {
    }
}
