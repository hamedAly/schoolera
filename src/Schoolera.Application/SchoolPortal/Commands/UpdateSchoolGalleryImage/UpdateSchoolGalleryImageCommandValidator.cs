using FluentValidation;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolGalleryImage;

public sealed class UpdateSchoolGalleryImageCommandValidator : AbstractValidator<UpdateSchoolGalleryImageCommand>
{
    public UpdateSchoolGalleryImageCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
    }
}
