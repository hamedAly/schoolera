using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(localizer["Required"])
            .EmailAddress().WithMessage(localizer["InvalidEmail"]);

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(localizer["Required"]);
    }
}
