using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Email;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Identity;

public sealed class VerificationCodeService(
    SchooleraDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<AuthMessages> authMessages,
    IOptions<AuthOptions> authOptions,
    ITransactionalEmailSender emailSender,
    ILogger<VerificationCodeService> logger) : IVerificationCodeService
{
    private const string RegistrationPurpose = "registration";

    public async Task<VerificationIssueResult> IssueRegistrationCodeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException($"Cannot issue verification code for unknown user '{userId}'.");

        var options = authOptions.Value.Verification;
        var code = GenerateNumericCode(options.CodeLength);
        var now = DateTimeOffset.UtcNow;

        var activeCodes = await dbContext.VerificationCodes
            .Where(entry => entry.UserId == userId && entry.Purpose == RegistrationPurpose && !entry.IsConsumed)
            .ToListAsync(cancellationToken);

        foreach (var activeCode in activeCodes)
        {
            activeCode.IsConsumed = true;
        }

        dbContext.VerificationCodes.Add(new VerificationCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Purpose = RegistrationPurpose,
            CodeHash = VerificationCodeHasher.Hash(code),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(options.ExpirationMinutes),
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var deliverySucceeded = await TryDeliverAsync(user, code, options.ExpirationMinutes, cancellationToken);
        return new VerificationIssueResult(
            deliverySucceeded,
            emailSender.Mode,
            options.ExpirationMinutes);
    }

    public async Task<Result<MessageResultDto>> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return Failure(authMessages["InvalidVerificationCode"].Value, AuthErrorCodes.InvalidVerificationCode);
        }

        if (user.EmailConfirmed || user.AccountStatus == AccountStatus.Active)
        {
            return Failure(authMessages["EmailAlreadyVerified"].Value, AuthErrorCodes.EmailAlreadyVerified);
        }

        var normalizedCode = code.Trim();
        var latest = await dbContext.VerificationCodes
            .Where(candidate =>
                candidate.UserId == user.Id &&
                candidate.Purpose == RegistrationPurpose)
            .OrderByDescending(candidate => candidate.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null || !VerificationCodeHasher.Verify(normalizedCode, latest.CodeHash))
        {
            return Failure(authMessages["InvalidVerificationCode"].Value, AuthErrorCodes.InvalidVerificationCode);
        }

        if (latest.IsConsumed)
        {
            return Failure(authMessages["CodeAlreadyUsed"].Value, AuthErrorCodes.CodeAlreadyUsed);
        }

        if (latest.ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            return Failure(authMessages["ExpiredVerificationCode"].Value, AuthErrorCodes.ExpiredVerificationCode);
        }

        latest.IsConsumed = true;
        user.AccountStatus = AccountStatus.Active;
        user.EmailConfirmed = true;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<MessageResultDto>.Success(
            new MessageResultDto(authMessages["VerificationSuccess"].Value));
    }

    public async Task<Result<MessageResultDto>> ResendAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await FindUserByEmailAsync(email, cancellationToken);
        if (user is null || user.AccountStatus != AccountStatus.PendingVerification)
        {
            // Avoid account enumeration — same success shape whether or not a code was issued.
            return Result<MessageResultDto>.Success(
                new MessageResultDto(authMessages["ResendSuccess"].Value));
        }

        var cooldown = authOptions.Value.Verification.ResendCooldownSeconds;
        var latest = await dbContext.VerificationCodes
            .Where(entry => entry.UserId == user.Id && entry.Purpose == RegistrationPurpose)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is not null &&
            latest.CreatedAtUtc.AddSeconds(cooldown) > DateTimeOffset.UtcNow)
        {
            return Failure(authMessages["ResendTooSoon"].Value, AuthErrorCodes.ResendTooSoon);
        }

        var issue = await IssueRegistrationCodeAsync(user.Id, cancellationToken);
        if (!issue.DeliverySucceeded)
        {
            return Failure(authMessages["DeliveryFailed"].Value, AuthErrorCodes.DeliveryFailed);
        }

        return Result<MessageResultDto>.Success(
            new MessageResultDto(
                authMessages["ResendSuccess"].Value,
                DeliverySucceeded: true,
                DeliveryMode: issue.DeliveryMode));
    }

    private async Task<bool> TryDeliverAsync(
        ApplicationUser user,
        string code,
        int expirationMinutes,
        CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendVerificationCodeAsync(
                new VerificationEmailMessage(
                    user.Email ?? string.Empty,
                    $"{user.FirstName} {user.LastName}".Trim(),
                    code,
                    expirationMinutes,
                    user.PreferredLanguage),
                cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to deliver verification email to {RecipientEmail} using mode {DeliveryMode}.",
                user.Email,
                emailSender.Mode);
            return false;
        }
    }

    private async Task<ApplicationUser?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = userManager.NormalizeEmail(email);
        return await userManager.Users
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    private static string GenerateNumericCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{length}");
    }

    private static Result<MessageResultDto> Failure(string message, string errorCode)
    {
        return Result<MessageResultDto>.Failure([message], [errorCode]);
    }
}
