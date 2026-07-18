namespace Schoolera.Application.Auth.Dtos;

public sealed record RegisterResultDto(
    Guid UserId,
    string Email,
    string AccountStatus,
    bool RequiresVerification,
    bool VerificationDeliverySucceeded,
    string VerificationDeliveryMode,
    int CodeExpiresInMinutes);
