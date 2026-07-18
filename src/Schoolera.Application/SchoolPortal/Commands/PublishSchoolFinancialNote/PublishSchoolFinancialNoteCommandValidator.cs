using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.PublishSchoolFinancialNote;

public sealed class PublishSchoolFinancialNoteCommandValidator : AbstractValidator<PublishSchoolFinancialNoteCommand>
{
    public PublishSchoolFinancialNoteCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}
