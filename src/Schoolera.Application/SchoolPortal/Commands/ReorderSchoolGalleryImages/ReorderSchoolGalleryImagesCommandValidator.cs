using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.ReorderSchoolGalleryImages;

public sealed class ReorderSchoolGalleryImagesCommandValidator : AbstractValidator<ReorderSchoolGalleryImagesCommand>
{
    public ReorderSchoolGalleryImagesCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
