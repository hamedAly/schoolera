using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolFinancialNote;

public sealed class UpdateSchoolFinancialNoteCommandValidator
    : AbstractValidator<UpdateSchoolFinancialNoteCommand>
{
    public UpdateSchoolFinancialNoteCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.NoteId).NotEmpty();
        RuleFor(command => command.Body.TextAr).NotEmpty().MaximumLength(1000);
    }
}
