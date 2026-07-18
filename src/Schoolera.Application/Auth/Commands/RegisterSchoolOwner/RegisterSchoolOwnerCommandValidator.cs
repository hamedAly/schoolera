using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Auth.Commands.RegisterSchoolOwner;

public sealed class RegisterSchoolOwnerCommandValidator : AbstractValidator<RegisterSchoolOwnerCommand>
{
    public RegisterSchoolOwnerCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.FirstName)
            .NotEmpty().WithMessage(localizer["Required"])
            .MaximumLength(FieldLengthLimits.UserFirstName);

        RuleFor(command => command.LastName)
            .NotEmpty().WithMessage(localizer["Required"])
            .MaximumLength(FieldLengthLimits.UserLastName);

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage(localizer["Required"])
            .EmailAddress().WithMessage(localizer["InvalidEmail"]);

        RuleFor(command => command.PhoneNumber)
            .NotEmpty().WithMessage(localizer["Required"])
            .MaximumLength(FieldLengthLimits.UserPhone);

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage(localizer["Required"])
            .MinimumLength(8).WithMessage(localizer["PasswordMinLength"]);

        RuleFor(command => command.ConfirmPassword)
            .Equal(command => command.Password).WithMessage(localizer["PasswordMismatch"]);

        RuleFor(command => command.TermsAccepted)
            .Equal(true).WithMessage(localizer["TermsRequired"]);

        RuleFor(command => command.PreferredLanguage)
            .NotEmpty().WithMessage(localizer["Required"])
            .MaximumLength(FieldLengthLimits.PreferredLanguage);
    }
}
