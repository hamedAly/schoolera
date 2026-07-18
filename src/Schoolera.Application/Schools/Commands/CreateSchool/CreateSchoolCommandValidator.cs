using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Schools.Commands.CreateSchool;

public sealed class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
{
    public CreateSchoolCommandValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage(_ => localizer["SchoolNameRequired"].Value)
            .MaximumLength(FieldLengthLimits.SchoolName)
            .WithMessage(_ => localizer["SchoolNameMaxLength", FieldLengthLimits.SchoolName].Value);

        RuleFor(command => command.City)
            .MaximumLength(FieldLengthLimits.SchoolCity)
            .WithMessage(_ => localizer["SchoolCityMaxLength", FieldLengthLimits.SchoolCity].Value);
    }
}
