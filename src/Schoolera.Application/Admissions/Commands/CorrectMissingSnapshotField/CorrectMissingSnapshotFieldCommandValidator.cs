using FluentValidation;

namespace Schoolera.Application.Admissions.Commands.CorrectMissingSnapshotField;

public sealed class CorrectMissingSnapshotFieldCommandValidator
    : AbstractValidator<CorrectMissingSnapshotFieldCommand>
{
    public CorrectMissingSnapshotFieldCommandValidator()
    {
        RuleFor(command => command.ApplicationId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.MissingItemId).NotEmpty();
    }
}
