using FluentValidation;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Domain.Common;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Schools.Commands.SubmitSchoolContactLead;

public sealed class SubmitSchoolContactLeadCommandValidator : AbstractValidator<SubmitSchoolContactLeadCommand>
{
    public SubmitSchoolContactLeadCommandValidator()
    {
        RuleFor(command => command.Slug)
            .NotEmpty()
            .Must(SlugHelper.IsValidSlug);

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

        RuleFor(command => command.Body.Message)
            .MaximumLength(FieldLengthLimits.Notes)
            .When(command => command.Body.Message is not null);

        RuleFor(command => command.Body.ConsentAccepted)
            .Equal(true)
            .WithErrorCode(SchoolErrorCodes.ContactConsentRequired)
            .WithMessage("Consent is required.");

        RuleFor(command => command.Body.Source)
            .Must(source => SchoolContactLeadSources.Normalize(source) is not null)
            .WithErrorCode(SchoolErrorCodes.ContactInvalidSource)
            .WithMessage("Source is not allowed.");

        RuleFor(command => command.Body.Website)
            .Empty()
            .WithErrorCode(SchoolErrorCodes.ContactRejected)
            .WithMessage("Unable to submit the request.");
    }
}
