using FluentValidation;

namespace Schoolera.Application.Favorites.Commands.RemoveFavoriteSchool;

public sealed class RemoveFavoriteSchoolCommandValidator : AbstractValidator<RemoveFavoriteSchoolCommand>
{
    public RemoveFavoriteSchoolCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
