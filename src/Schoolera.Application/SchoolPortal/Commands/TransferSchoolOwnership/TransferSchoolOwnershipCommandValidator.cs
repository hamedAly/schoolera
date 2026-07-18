using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.TransferSchoolOwnership;

public sealed class TransferSchoolOwnershipCommandValidator : AbstractValidator<TransferSchoolOwnershipCommand>
{
    public TransferSchoolOwnershipCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.NewOwnerEmail).NotEmpty().MaximumLength(256).EmailAddress();
    }
}
