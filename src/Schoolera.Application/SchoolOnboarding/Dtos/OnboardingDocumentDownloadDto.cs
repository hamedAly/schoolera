namespace Schoolera.Application.SchoolOnboarding.Dtos;

/// <summary>
/// Carries a private document stream to the protected download endpoint. The stream is
/// owned by the caller (controller) and must be disposed after the response is written.
/// </summary>
public sealed record OnboardingDocumentDownloadDto(
    Stream Content,
    string ContentType,
    string DownloadFileName);
