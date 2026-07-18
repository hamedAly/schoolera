using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.RegisterParent;

public sealed record RegisterParentCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    bool TermsAccepted,
    bool PrivacyAccepted,
    string PreferredLanguage) : IRequest<Result<RegisterResultDto>>;

public sealed class RegisterParentCommandHandler(IAuthAccountService authAccountService, ILogger<RegisterParentCommandHandler> logger)
    : IRequestHandler<RegisterParentCommand, Result<RegisterResultDto>>
{
    public Task<Result<RegisterResultDto>> Handle(
        RegisterParentCommand request,
        CancellationToken cancellationToken)
    {
        var model = new RegisterAccountModel(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.TermsAccepted,
            request.PrivacyAccepted,
            request.PreferredLanguage);

        return authAccountService.RegisterParentAsync(model, cancellationToken);
    }
}
