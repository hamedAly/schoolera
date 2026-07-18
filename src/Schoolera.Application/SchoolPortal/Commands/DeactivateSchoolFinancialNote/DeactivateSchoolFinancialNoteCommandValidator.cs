using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolFinancialNote;

public sealed class DeactivateSchoolFinancialNoteCommandValidator : AbstractValidator<DeactivateSchoolFinancialNoteCommand>
{
    public DeactivateSchoolFinancialNoteCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
