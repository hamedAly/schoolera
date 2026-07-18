using MediatR;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<CurrentUserDto?>>;

public sealed class GetCurrentUserQueryHandler(IAuthAccountService authAccountService)
    : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto?>>
{
    public Task<Result<CurrentUserDto?>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return authAccountService.GetCurrentUserAsync(cancellationToken);
    }
}
