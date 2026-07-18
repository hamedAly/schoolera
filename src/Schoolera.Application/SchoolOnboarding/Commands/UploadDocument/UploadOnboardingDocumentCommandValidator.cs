using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.SchoolOnboarding.Commands.UploadDocument;

public sealed class UploadOnboardingDocumentCommandValidator
    : AbstractValidator<UploadOnboardingDocumentCommand>
{
    public UploadOnboardingDocumentCommandValidator(IStringLocalizer<OnboardingMessages> localizer)
    {
        RuleFor(command => command.DocumentTypeId)
            .NotEmpty()
            .WithMessage(localizer["InvalidDocumentType"]);

        RuleFor(command => command.OriginalFileName)
            .NotEmpty()
            .WithMessage(localizer["UnsupportedDocumentFormat"]);
    }
}
