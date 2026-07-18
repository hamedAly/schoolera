namespace Schoolera.Application.Auth.Dtos;

public sealed record MessageResultDto(
    string Message,
    bool? DeliverySucceeded = null,
    string? DeliveryMode = null);
