using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.Verify;

public sealed record VerifyCommand(string Email, string Code) : IRequest<Result<MessageResultDto>>;

public sealed class VerifyCommandHandler(IVerificationCodeService verificationCodeService, ILogger<VerifyCommandHandler> logger)
    : IRequestHandler<VerifyCommand, Result<MessageResultDto>>
{
    public Task<Result<MessageResultDto>> Handle(VerifyCommand request, CancellationToken cancellationToken)
    {
        return verificationCodeService.VerifyAsync(request.Email, request.Code, cancellationToken);
    }
}
