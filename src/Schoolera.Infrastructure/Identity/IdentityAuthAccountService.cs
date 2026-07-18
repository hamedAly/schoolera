using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Legal.Constants;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Identity;

public sealed class IdentityAuthAccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    SchooleraDbContext dbContext,
    IVerificationCodeService verificationCodeService,
    ILegalConsentService legalConsentService,
    IStringLocalizer<AuthMessages> authMessages,
    IOptions<AuthOptions> authOptions) : IAuthAccountService
{
    public Task<Result<RegisterResultDto>> RegisterParentAsync(
        RegisterAccountModel model,
        CancellationToken cancellationToken = default)
    {
        return RegisterAsync(model, SchooleraRoles.Parent, cancellationToken);
    }

    public Task<Result<RegisterResultDto>> RegisterSchoolOwnerAsync(
        RegisterAccountModel model,
        CancellationToken cancellationToken = default)
    {
        return RegisterAsync(model, SchooleraRoles.SchoolOwner, cancellationToken);
    }

    public async Task<Result<CurrentUserDto>> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userManager.NormalizeEmail(email);
        var user = await userManager.Users
            .FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Failure<CurrentUserDto>(
                authMessages["InvalidCredentials"].Value,
                AuthErrorCodes.InvalidCredentials);
        }

        if (user.AccountStatus == AccountStatus.Suspended)
        {
            return Failure<CurrentUserDto>(
                authMessages["AccountSuspended"].Value,
                AuthErrorCodes.AccountSuspended);
        }

        if (user.AccountStatus == AccountStatus.PendingVerification)
        {
            return Failure<CurrentUserDto>(
                authMessages["AccountNotVerified"].Value,
                AuthErrorCodes.AccountNotVerified);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return Failure<CurrentUserDto>(
                authMessages["InvalidCredentials"].Value,
                AuthErrorCodes.InvalidCredentials);
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return Result<CurrentUserDto>.Success(await MapCurrentUserAsync(user, cancellationToken));
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        return signInManager.SignOutAsync();
    }

    public async Task<Result<CurrentUserDto?>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = signInManager.Context.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Result<CurrentUserDto?>.Success(null);
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Result<CurrentUserDto?>.Success(null);
        }

        return Result<CurrentUserDto?>.Success(await MapCurrentUserAsync(user, cancellationToken));
    }

    private async Task<Result<RegisterResultDto>> RegisterAsync(
        RegisterAccountModel model,
        string role,
        CancellationToken cancellationToken)
    {
        if (!SchooleraRoles.PublicRegistration.Contains(role, StringComparer.Ordinal))
        {
            return Failure<RegisterResultDto>(
                authMessages["Forbidden"].Value,
                AuthErrorCodes.Forbidden);
        }

        var persistParentConsent = string.Equals(role, SchooleraRoles.Parent, StringComparison.Ordinal);
        if (persistParentConsent)
        {
            if (!model.TermsAccepted)
            {
                return Failure<RegisterResultDto>(
                    "Terms acceptance is required.",
                    LegalErrorCodes.TermsRequired);
            }

            if (!model.PrivacyAccepted)
            {
                return Failure<RegisterResultDto>(
                    "Privacy acceptance is required.",
                    LegalErrorCodes.PrivacyRequired);
            }

            var ensure = await legalConsentService.EnsureCurrentVersionsExistAsync(cancellationToken);
            if (!ensure.Succeeded)
            {
                return Result<RegisterResultDto>.Failure(ensure.Errors, ensure.ErrorCodes);
            }
        }

        var normalizedEmail = userManager.NormalizeEmail(model.Email);
        if (await userManager.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken))
        {
            return Failure<RegisterResultDto>(
                authMessages["DuplicateEmail"].Value,
                AuthErrorCodes.EmailAlreadyExists);
        }

        if (await userManager.Users.AnyAsync(
                user => user.PhoneNumber != null && user.PhoneNumber == model.PhoneNumber,
                cancellationToken))
        {
            return Failure<RegisterResultDto>(
                authMessages["DuplicatePhone"].Value,
                AuthErrorCodes.DuplicatePhone);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            PreferredLanguage = NormalizePreferredLanguage(model.PreferredLanguage),
            AccountStatus = AccountStatus.PendingVerification,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            EmailConfirmed = false,
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return IdentityErrorMapper.ToFailureResult<RegisterResultDto>(
                createResult.Errors,
                authMessages);
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            return IdentityErrorMapper.ToFailureResult<RegisterResultDto>(
                roleResult.Errors,
                authMessages);
        }

        if (persistParentConsent)
        {
            var consent = await legalConsentService.PersistCurrentAcceptancesAsync(
                user.Id,
                LegalAcceptancePurpose.ParentRegistration,
                model.TermsAccepted,
                model.PrivacyAccepted,
                cancellationToken);
            if (!consent.Succeeded)
            {
                return Result<RegisterResultDto>.Failure(consent.Errors, consent.ErrorCodes);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var issue = await verificationCodeService.IssueRegistrationCodeAsync(user.Id, cancellationToken);

        return Result<RegisterResultDto>.Success(new RegisterResultDto(
            user.Id,
            user.Email!,
            user.AccountStatus.ToString(),
            RequiresVerification: true,
            issue.DeliverySucceeded,
            issue.DeliveryMode,
            issue.CodeExpiresInMinutes));
    }

    private async Task<CurrentUserDto> MapCurrentUserAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            roles.ToArray(),
            user.AccountStatus.ToString(),
            user.PreferredLanguage,
            PostLoginDestinations.ForRoles(roles));
    }

    private static string NormalizePreferredLanguage(string preferredLanguage)
    {
        return preferredLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
    }

    private static Result<T> Failure<T>(string message, string errorCode)
    {
        return Result<T>.Failure([message], [errorCode]);
    }
}
