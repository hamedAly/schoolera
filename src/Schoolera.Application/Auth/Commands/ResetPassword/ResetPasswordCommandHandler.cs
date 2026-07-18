using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result<MessageResultDto>>;

public sealed class ResetPasswordCommandHandler(IPasswordResetService passwordResetService, ILogger<ResetPasswordCommandHandler> logger)
    : IRequestHandler<ResetPasswordCommand, Result<MessageResultDto>>
{
    public Task<Result<MessageResultDto>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        return passwordResetService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            request.ConfirmPassword,
            cancellationToken);
    }
}
