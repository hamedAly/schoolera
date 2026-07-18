using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.DeleteSchoolGalleryImage;

public sealed class DeleteSchoolGalleryImageCommandValidator : AbstractValidator<DeleteSchoolGalleryImageCommand>
{
    public DeleteSchoolGalleryImageCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
