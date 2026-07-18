using FluentValidation;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.UploadChildDocument;

public sealed class UploadChildDocumentCommandValidator : AbstractValidator<UploadChildDocumentCommand>
{
    public UploadChildDocumentCommandValidator()
    {
        RuleFor(command => command.ChildId).NotEmpty();
        RuleFor(command => command.DocumentType).IsInEnum();
        RuleFor(command => command.Content).NotNull();
        RuleFor(command => command.OriginalFileName).NotEmpty().MaximumLength(260);
        RuleFor(command => command.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(command => command.FileSize).GreaterThan(0);
    }
}
