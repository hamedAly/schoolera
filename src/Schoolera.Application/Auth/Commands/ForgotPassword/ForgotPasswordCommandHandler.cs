using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<MessageResultDto>>;

public sealed class ForgotPasswordCommandHandler(IPasswordResetService passwordResetService, ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<MessageResultDto>>
{
    public Task<Result<MessageResultDto>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        return passwordResetService.RequestResetAsync(request.Email, cancellationToken);
    }
}
