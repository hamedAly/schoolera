using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(localizer["Required"])
            .EmailAddress().WithMessage(localizer["InvalidEmail"]);

        RuleFor(command => command.Token)
            .NotEmpty().WithMessage(localizer["Required"]);

        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage(localizer["Required"])
            .MinimumLength(8).WithMessage(localizer["PasswordMinLength"]);

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.NewPassword).WithMessage(localizer["PasswordMismatch"]);
    }
}
