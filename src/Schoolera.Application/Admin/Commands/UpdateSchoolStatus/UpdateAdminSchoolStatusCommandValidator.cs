using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Commands.UpdateSchoolStatus;

public sealed class UpdateAdminSchoolStatusCommandValidator : AbstractValidator<UpdateAdminSchoolStatusCommand>
{
    public UpdateAdminSchoolStatusCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Status)
            .NotEmpty().WithMessage(localizer["Required"]);
    }
}
