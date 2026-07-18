using FluentValidation;

namespace Schoolera.Application.Parent.Commands.DeleteChildProfile;

public sealed class DeleteChildProfileCommandValidator : AbstractValidator<DeleteChildProfileCommand>
{
    public DeleteChildProfileCommandValidator()
    {
        RuleFor(command => command.ChildId).NotEmpty();
    }
}
