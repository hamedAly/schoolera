using FluentValidation;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.CreateChildProfile;

public sealed class CreateChildProfileCommandValidator : AbstractValidator<CreateChildProfileCommand>
{
    public CreateChildProfileCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.FullName)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.PersonName);
        RuleFor(command => command.Body.IdentityType).IsInEnum();
        RuleFor(command => command.Body.IdentityValue).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Body.Gender).IsInEnum();
        RuleFor(command => command.Body.CurrentGradeId).NotEmpty();
        RuleFor(command => command.Body.SpecialNeedsNotes)
            .MaximumLength(FieldLengthLimits.Notes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.SpecialNeedsNotes));
        RuleFor(command => command.Body.HealthNotes)
            .MaximumLength(FieldLengthLimits.Notes)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.HealthNotes));
        RuleFor(command => command.Body.CurrentSchoolName)
            .MaximumLength(FieldLengthLimits.ChildCurrentSchoolName)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.CurrentSchoolName));
        RuleFor(command => command.Body.PreferredStudyLanguage)
            .IsInEnum()
            .When(command => command.Body.PreferredStudyLanguage is not null);
        RuleFor(command => command.Body.Skills)
            .MaximumLength(FieldLengthLimits.ChildFreeText)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Skills));
        RuleFor(command => command.Body.Hobbies)
            .MaximumLength(FieldLengthLimits.ChildFreeText)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Hobbies));
        RuleFor(command => command.Body.Strengths)
            .MaximumLength(FieldLengthLimits.ChildFreeText)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Strengths));
        RuleFor(command => command.Body.ImprovementAreas)
            .MaximumLength(FieldLengthLimits.ChildFreeText)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.ImprovementAreas));
    }
}
