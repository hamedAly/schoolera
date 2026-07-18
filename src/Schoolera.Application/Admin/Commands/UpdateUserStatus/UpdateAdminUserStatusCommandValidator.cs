using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Commands.UpdateUserStatus;

public sealed class UpdateAdminUserStatusCommandValidator : AbstractValidator<UpdateAdminUserStatusCommand>
{
    public UpdateAdminUserStatusCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.AccountStatus)
            .NotEmpty().WithMessage(localizer["Required"]);
    }
}
