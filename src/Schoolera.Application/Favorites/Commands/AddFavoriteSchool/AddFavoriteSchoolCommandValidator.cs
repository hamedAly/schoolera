using FluentValidation;

namespace Schoolera.Application.Favorites.Commands.AddFavoriteSchool;

public sealed class AddFavoriteSchoolCommandValidator : AbstractValidator<AddFavoriteSchoolCommand>
{
    public AddFavoriteSchoolCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
