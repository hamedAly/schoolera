using FluentValidation;
using Schoolera.Domain.Common;

namespace Schoolera.Application.Admissions.Commands.GrantAdmissionAgeEligibilityException;

public sealed class GrantAdmissionAgeEligibilityExceptionCommandValidator
    : AbstractValidator<GrantAdmissionAgeEligibilityExceptionCommand>
{
    public GrantAdmissionAgeEligibilityExceptionCommandValidator()
    {
        RuleFor(x => x.SchoolId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.ReasonCode).IsInEnum();
        RuleFor(x => x.Body.ReasonNote)
            .MaximumLength(FieldLengthLimits.AgeEligibilityExceptionNote)
            .When(x => x.Body.ReasonNote is not null);
    }
}
