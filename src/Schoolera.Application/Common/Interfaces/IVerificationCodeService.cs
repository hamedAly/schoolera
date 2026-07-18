using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Common.Interfaces;

public interface IVerificationCodeService
{
    Task<VerificationIssueResult> IssueRegistrationCodeAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<MessageResultDto>> VerifyAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);

    Task<Result<MessageResultDto>> ResendAsync(
        string email,
        CancellationToken cancellationToken = default);
}
