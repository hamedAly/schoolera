using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Application.Auth.Commands.Logout;

public sealed record LogoutCommand : IRequest;

public sealed class LogoutCommandHandler(IAuthAccountService authAccountService, ILogger<LogoutCommandHandler> logger)
    : IRequestHandler<LogoutCommand>
{
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        return authAccountService.SignOutAsync(cancellationToken);
    }
}
