using FluentValidation;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Cms.Commands.SubmitContactRequest;

public sealed class SubmitContactRequestCommandValidator : AbstractValidator<SubmitContactRequestCommand>
{
    public SubmitContactRequestCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();

        RuleFor(command => command.Body.Name)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.PersonName);

        RuleFor(command => command.Body.Phone)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Phone);

        RuleFor(command => command.Body.Email)
            .MaximumLength(FieldLengthLimits.Email)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Email));

        RuleFor(command => command.Body.Subject)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.ContactSubject);

        RuleFor(command => command.Body.Message)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.ContactMessage);

        RuleFor(command => command.Body.ConsentAccepted)
            .Equal(true)
            .WithErrorCode(ContactErrorCodes.ConsentRequired);

        RuleFor(command => command.Body.Category)
            .Must(category => ContactCategories.Normalize(category) is not null)
            .WithErrorCode(ContactErrorCodes.InvalidCategory);

        RuleFor(command => command.Body.Source)
            .Must(source => ContactSources.Normalize(source) is not null)
            .WithErrorCode(ContactErrorCodes.InvalidSource);

        RuleFor(command => command.Body.Website)
            .Empty()
            .WithErrorCode(ContactErrorCodes.Rejected);
    }
}
