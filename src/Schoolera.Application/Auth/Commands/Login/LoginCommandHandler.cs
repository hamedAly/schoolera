using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<CurrentUserDto>>;

public sealed class LoginCommandHandler(
    IAuthAccountService authAccountService,
    ILogger<LoginCommandHandler> logger) : IRequestHandler<LoginCommand, Result<CurrentUserDto>>
{
    public Task<Result<CurrentUserDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return authAccountService.SignInAsync(request.Email, request.Password, cancellationToken);
    }
}
