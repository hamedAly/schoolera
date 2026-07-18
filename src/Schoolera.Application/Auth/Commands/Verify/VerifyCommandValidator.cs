using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Auth.Commands.Verify;

public sealed class VerifyCommandValidator : AbstractValidator<VerifyCommand>
{
    public VerifyCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(localizer["Required"])
            .EmailAddress().WithMessage(localizer["InvalidEmail"]);

        RuleFor(command => command.Code)
            .NotEmpty().WithMessage(localizer["Required"])
            .Length(6).WithMessage(localizer["VerificationCodeLength"]);
    }
}
