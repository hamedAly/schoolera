using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Common.Interfaces;

public interface IPasswordResetService
{
    Task<Result<MessageResultDto>> RequestResetAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<Result<MessageResultDto>> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        string confirmPassword,
        CancellationToken cancellationToken = default);
}
