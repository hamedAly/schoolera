using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ActivateSchoolFinancialNote;

public sealed class ActivateSchoolFinancialNoteCommandValidator : AbstractValidator<ActivateSchoolFinancialNoteCommand>
{
    public ActivateSchoolFinancialNoteCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
