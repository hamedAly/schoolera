using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolFinancialNote;

public sealed class CreateSchoolFinancialNoteCommandValidator
    : AbstractValidator<CreateSchoolFinancialNoteCommand>
{
    public CreateSchoolFinancialNoteCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.Body.TextAr).NotEmpty().MaximumLength(1000);
    }
}
