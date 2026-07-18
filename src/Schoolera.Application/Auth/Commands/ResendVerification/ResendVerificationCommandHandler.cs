using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.ResendVerification;

public sealed record ResendVerificationCommand(string Email) : IRequest<Result<MessageResultDto>>;

public sealed class ResendVerificationCommandHandler(IVerificationCodeService verificationCodeService, ILogger<ResendVerificationCommandHandler> logger)
    : IRequestHandler<ResendVerificationCommand, Result<MessageResultDto>>
{
    public Task<Result<MessageResultDto>> Handle(
        ResendVerificationCommand request,
        CancellationToken cancellationToken)
    {
        return verificationCodeService.ResendAsync(request.Email, cancellationToken);
    }
}
