using Schoolera.Application.Auth.Dtos;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Common.Interfaces;

public interface IAuthAccountService
{
    Task<Result<RegisterResultDto>> RegisterParentAsync(
        RegisterAccountModel model,
        CancellationToken cancellationToken = default);

    Task<Result<RegisterResultDto>> RegisterSchoolOwnerAsync(
        RegisterAccountModel model,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto>> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto?>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public sealed record RegisterAccountModel(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    bool TermsAccepted,
    bool PrivacyAccepted,
    string PreferredLanguage);
