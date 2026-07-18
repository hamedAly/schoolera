using FluentValidation;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Resources;

namespace Schoolera.Application.SchoolOnboarding.Commands.DeleteDocument;

public sealed class DeleteOnboardingDocumentCommandValidator
    : AbstractValidator<DeleteOnboardingDocumentCommand>
{
    public DeleteOnboardingDocumentCommandValidator(IStringLocalizer<OnboardingMessages> localizer)
    {
        RuleFor(command => command.DocumentId)
            .NotEmpty()
            .WithMessage(localizer["NotFound"]);
    }
}
