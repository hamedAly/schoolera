namespace Schoolera.Application.Auth.Dtos;

public sealed record VerificationIssueResult(
    bool DeliverySucceeded,
    string DeliveryMode,
    int CodeExpiresInMinutes);
