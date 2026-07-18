using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Auth.Commands.RegisterSchoolOwner;

public sealed record RegisterSchoolOwnerCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    string ConfirmPassword,
    bool TermsAccepted,
    string PreferredLanguage) : IRequest<Result<RegisterResultDto>>;

public sealed class RegisterSchoolOwnerCommandHandler(IAuthAccountService authAccountService, ILogger<RegisterSchoolOwnerCommandHandler> logger)
    : IRequestHandler<RegisterSchoolOwnerCommand, Result<RegisterResultDto>>
{
    public Task<Result<RegisterResultDto>> Handle(
        RegisterSchoolOwnerCommand request,
        CancellationToken cancellationToken)
    {
        var model = new RegisterAccountModel(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Password,
            request.TermsAccepted,
            PrivacyAccepted: true,
            request.PreferredLanguage);

        return authAccountService.RegisterSchoolOwnerAsync(model, cancellationToken);
    }
}
