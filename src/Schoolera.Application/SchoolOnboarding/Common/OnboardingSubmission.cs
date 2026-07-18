using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolOnboarding.Common;

/// <summary>Full aggregate validation run on submit and resubmit.</summary>
public static class OnboardingSubmission
{
    public static async Task<IReadOnlyList<string>> GetBlockingCodesAsync(
        SchoolOnboardingApplication application,
        ISchoolOnboardingRepository repository,
        CancellationToken cancellationToken)
    {
        var documentTypes = await repository.ListActiveDocumentTypesAsync(cancellationToken);
        var requiredIds = documentTypes.Where(t => t.IsRequired).Select(t => t.Id).ToArray();
        var currentIds = application.Documents.Where(d => d.IsCurrent).Select(d => d.DocumentTypeId).ToArray();

        var districtBelongsToCity = true;
        if (application.CityId is { } cityId && application.DistrictId is { } districtId)
        {
            districtBelongsToCity =
                await repository.DistrictBelongsToCityAsync(districtId, cityId, cancellationToken);
        }

        var codes = OnboardingCompleteness
            .GetSubmissionErrorCodes(application, requiredIds, currentIds, districtBelongsToCity)
            .ToList();

        if (application.NormalizedRegistrationNumber is { } normalized &&
            !string.IsNullOrWhiteSpace(application.CountryCode) &&
            await repository.RegistrationNumberExistsAsync(
                normalized, application.CountryCode, application.Id, cancellationToken))
        {
            codes.Add(OnboardingErrorCodes.DuplicateRegistrationNumber);
        }

        return codes.Distinct().ToArray();
    }
}
