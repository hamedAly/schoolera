using FluentValidation;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.ReplaceChildDocument;

public sealed class ReplaceChildDocumentCommandValidator : AbstractValidator<ReplaceChildDocumentCommand>
{
    public ReplaceChildDocumentCommandValidator()
    {
        RuleFor(command => command.ChildId).NotEmpty();
        RuleFor(command => command.DocumentId).NotEmpty();
        RuleFor(command => command.DocumentType).IsInEnum();
        RuleFor(command => command.Content).NotNull();
        RuleFor(command => command.OriginalFileName).NotEmpty().MaximumLength(260);
        RuleFor(command => command.ContentType).NotEmpty().MaximumLength(128);
        RuleFor(command => command.FileSize).GreaterThan(0);
    }
}
