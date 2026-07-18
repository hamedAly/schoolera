using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(localizer["Required"])
            .EmailAddress().WithMessage(localizer["InvalidEmail"]);
    }
}
