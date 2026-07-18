using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Infrastructure.Identity;

public sealed class PasswordResetService(
    UserManager<ApplicationUser> userManager,
    IStringLocalizer<AuthMessages> authMessages,
    IOptions<AuthOptions> authOptions,
    IHostEnvironment hostEnvironment,
    ILogger<PasswordResetService> logger) : IPasswordResetService
{
    public async Task<Result<MessageResultDto>> RequestResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = userManager.NormalizeEmail(email);
        var user = await userManager.Users
            .FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is not null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            LogDevelopmentResetToken(user.Email!, token);
        }

        return Result<MessageResultDto>.Success(
            new MessageResultDto(authMessages["ForgotPasswordSuccess"].Value));
    }

    public async Task<Result<MessageResultDto>> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return Result<MessageResultDto>.Failure(
                [authMessages["InvalidResetToken"].Value],
                [AuthErrorCodes.InvalidResetToken]);
        }

        var normalizedEmail = userManager.NormalizeEmail(email);
        var user = await userManager.Users
            .FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return Result<MessageResultDto>.Failure(
                [authMessages["InvalidResetToken"].Value],
                [AuthErrorCodes.InvalidResetToken]);
        }

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return IdentityErrorMapper.ToFailureResult<MessageResultDto>(
                result.Errors,
                authMessages);
        }

        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return Result<MessageResultDto>.Success(
            new MessageResultDto(authMessages["ResetPasswordSuccess"].Value));
    }

    private void LogDevelopmentResetToken(string email, string token)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        logger.LogInformation(
            "Development password reset token for {Email}: {ResetToken}",
            email,
            token);
    }
}
