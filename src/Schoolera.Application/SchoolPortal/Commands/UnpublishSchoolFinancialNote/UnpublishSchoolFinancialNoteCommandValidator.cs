using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UnpublishSchoolFinancialNote;

public sealed class UnpublishSchoolFinancialNoteCommandValidator : AbstractValidator<UnpublishSchoolFinancialNoteCommand>
{
    public UnpublishSchoolFinancialNoteCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
