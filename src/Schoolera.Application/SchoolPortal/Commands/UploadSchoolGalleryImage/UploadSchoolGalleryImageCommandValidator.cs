using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UploadSchoolGalleryImage;

public sealed class UploadSchoolGalleryImageCommandValidator : AbstractValidator<UploadSchoolGalleryImageCommand>
{
    public UploadSchoolGalleryImageCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
