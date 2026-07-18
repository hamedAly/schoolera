using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Common;

/// <summary>Centralized construction of the owner-facing read model.</summary>
public static class OnboardingReadModel
{
    public static async Task<MyOnboardingApplicationDto> BuildAsync(
        SchoolOnboardingApplication application,
        ISchoolOnboardingRepository repository,
        CancellationToken cancellationToken)
    {
        var documentTypes = await repository.ListActiveDocumentTypesAsync(cancellationToken);
        var requiredIds = documentTypes.Where(t => t.IsRequired).Select(t => t.Id).ToArray();

        string? approvedSchoolName = application.ApprovedSchoolId is { } schoolId
            ? await repository.GetSchoolNameAsync(schoolId, cancellationToken)
            : null;

        return MyOnboardingApplicationDto.FromEntity(application, requiredIds, approvedSchoolName);
    }
}
