using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolOnboardingRepository
{
    Task<SchoolOnboardingApplication?> GetByOwnerAsync(
        Guid ownerUserId,
        bool includeChildren,
        CancellationToken cancellationToken = default);

    Task<SchoolOnboardingApplication?> GetByIdAsync(
        Guid id,
        bool includeChildren,
        CancellationToken cancellationToken = default);

    Task AddAsync(SchoolOnboardingApplication application, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolOnboardingDocumentType>> ListActiveDocumentTypesAsync(
        CancellationToken cancellationToken = default);

    Task<SchoolOnboardingDocumentType?> GetDocumentTypeByIdAsync(
        Guid documentTypeId,
        CancellationToken cancellationToken = default);

    Task<SchoolOnboardingDocument?> GetDocumentByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<bool> RegistrationNumberExistsAsync(
        string normalizedRegistrationNumber,
        string countryCode,
        Guid excludeApplicationId,
        CancellationToken cancellationToken = default);

    Task<bool> DistrictBelongsToCityAsync(
        Guid districtId,
        Guid cityId,
        CancellationToken cancellationToken = default);

    Task<string?> GetSchoolNameAsync(Guid schoolId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SchoolOnboardingApplication> Items, int TotalCount)> SearchAsync(
        SchoolOnboardingStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
