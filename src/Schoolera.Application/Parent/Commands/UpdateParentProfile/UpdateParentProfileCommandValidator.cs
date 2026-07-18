using FluentValidation;
using Schoolera.Domain.Common;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Parent.Commands.UpdateParentProfile;

public sealed class UpdateParentProfileCommandValidator : AbstractValidator<UpdateParentProfileCommand>
{
    public UpdateParentProfileCommandValidator()
    {
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.FirstName)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.UserFirstName);
        RuleFor(command => command.Body.LastName)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.UserLastName);
        RuleFor(command => command.Body.Phone)
            .NotEmpty()
            .MaximumLength(FieldLengthLimits.Phone);
        RuleFor(command => command.Body.AlternatePhone)
            .MaximumLength(FieldLengthLimits.Phone)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.AlternatePhone));
        RuleFor(command => command.Body.AddressLine)
            .MaximumLength(FieldLengthLimits.AddressLine)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.AddressLine));
        RuleFor(command => command.Body.Qualification)
            .MaximumLength(FieldLengthLimits.ParentQualification)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Qualification));
        RuleFor(command => command.Body.Occupation)
            .MaximumLength(FieldLengthLimits.ParentOccupation)
            .When(command => !string.IsNullOrWhiteSpace(command.Body.Occupation));
        RuleFor(command => command.Body.PreferredContactMethod)
            .IsInEnum();
        RuleFor(command => command.Body.PreferredLanguage)
            .NotEmpty()
            .Must(lang => lang is "ar" or "en")
            .WithMessage("Preferred language must be ar or en.");

        When(command => command.Body.Father is not null, () =>
        {
            RuleFor(command => command.Body.Father!.FullName)
                .MaximumLength(FieldLengthLimits.PersonName)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Father!.FullName));
            RuleFor(command => command.Body.Father!.Phone)
                .MaximumLength(FieldLengthLimits.Phone)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Father!.Phone));
            RuleFor(command => command.Body.Father!.Email)
                .EmailAddress()
                .MaximumLength(FieldLengthLimits.Email)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Father!.Email));
            RuleFor(command => command.Body.Father!.Occupation)
                .MaximumLength(FieldLengthLimits.ParentOccupation)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Father!.Occupation));
            RuleFor(command => command.Body.Father!.Qualification)
                .MaximumLength(FieldLengthLimits.ParentQualification)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Father!.Qualification));
            RuleFor(command => command.Body.Father!.IdentityType)
                .IsInEnum()
                .When(command => command.Body.Father!.IdentityType is not null);
            RuleFor(command => command.Body.Father!.IdentityValue)
                .MaximumLength(64)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Father!.IdentityValue));
        });

        When(command => command.Body.Mother is not null, () =>
        {
            RuleFor(command => command.Body.Mother!.FullName)
                .MaximumLength(FieldLengthLimits.PersonName)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Mother!.FullName));
            RuleFor(command => command.Body.Mother!.Phone)
                .MaximumLength(FieldLengthLimits.Phone)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Mother!.Phone));
            RuleFor(command => command.Body.Mother!.Email)
                .EmailAddress()
                .MaximumLength(FieldLengthLimits.Email)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Mother!.Email));
            RuleFor(command => command.Body.Mother!.Occupation)
                .MaximumLength(FieldLengthLimits.ParentOccupation)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Mother!.Occupation));
            RuleFor(command => command.Body.Mother!.Qualification)
                .MaximumLength(FieldLengthLimits.ParentQualification)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Mother!.Qualification));
            RuleFor(command => command.Body.Mother!.IdentityType)
                .IsInEnum()
                .When(command => command.Body.Mother!.IdentityType is not null);
            RuleFor(command => command.Body.Mother!.IdentityValue)
                .MaximumLength(64)
                .When(command => !string.IsNullOrWhiteSpace(command.Body.Mother!.IdentityValue));
        });
    }
}
