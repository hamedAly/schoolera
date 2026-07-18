using FluentValidation;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpsertSchoolTeamMember;

public sealed class UpsertSchoolTeamMemberCommandValidator : AbstractValidator<UpsertSchoolTeamMemberCommand>
{
    public UpsertSchoolTeamMemberCommandValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.Body).NotNull();
        RuleFor(command => command.Body.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(command => command.Body.Role).IsInEnum()
            .Must(role => role is >= SchoolTeamRole.SchoolAdmin and <= SchoolTeamRole.ContentModerator);
        RuleFor(command => command.Body.BranchScopeMode).IsInEnum();
    }
}
