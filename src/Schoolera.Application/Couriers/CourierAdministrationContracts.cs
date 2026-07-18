using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Couriers;

public static class CourierErrorCodes
{
    public const string Forbidden = "courier.forbidden";
    public const string NotFound = "courier.notFound";
    public const string NotCourier = "courier.integrationTypeRequired";
    public const string Conflict = "courier.configurationConflict";
    public const string InvalidHierarchy = "courier.invalidLocation";
    public const string InvalidTimeZone = "courier.invalidTimeZone";
    public const string InvalidWindow = "courier.invalidWindow";
    public const string OverlappingWindow = "courier.overlappingWindow";
    public const string DuplicateActiveScope = "courier.duplicateActiveScope";
    public const string InvalidOwnership = "courier.invalidOwnership";
    public const string InvalidSla = "courier.invalidSla";
    public const string ApplicationCancelled = "courier.applicationCancelled";
    public const string BranchInactive = "courier.destinationBranchInactive";
    public const string InvalidRequest = "courier.invalidRequest";
    public const string InvalidScope = "courier.invalidScope";
    public const string DropOffPointUnsupported = "courier.dropOffPointUnsupported";
}

public sealed record CourierProviderProfileDto(
    string DescriptionAr, string DescriptionEn, string? LogoReference, string TermsUrl,
    string PrivacyUrl, int ConfigurationVersion, string RowVersion);

public sealed record CourierServiceDto(
    Guid Id, CourierServiceType ServiceType, string Code, string NameAr, string NameEn, string DescriptionAr, string DescriptionEn,
    bool IsActive, int SortOrder, int MinimumPickupLeadTimeMinutes, TimeOnly DailyCutoffLocalTime,
    int MaximumFuturePickupDays, int AcceptanceWindowMinutes, bool SupportsScheduledPickup,
    bool SupportsSameDayPickup, bool CanCreatePickup, bool CanQueryStatus, bool SupportsWebhook,
    bool SupportsPolling, bool SupportsManualUpdates, bool SupportsCancellationBeforePickup,
    bool SupportsCourierAssignment, bool SupportsProofOfPickup, bool SupportsProofOfDelivery,
    bool SupportsDropOffPoint, int MaximumEnvelopeWeightGrams, decimal MaximumEnvelopeLengthCm,
    decimal MaximumEnvelopeWidthCm, decimal MaximumEnvelopeHeightCm, string RowVersion);

public sealed record CourierCoverageDto(
    Guid Id, Guid? ServiceId, Guid CountryId, Guid? GovernorateId, Guid? CityId, Guid? DistrictId,
    CourierCoverageResult Result, string? NotesAr, string? NotesEn, bool IsActive, string RowVersion);

public sealed record CourierWindowDto(
    Guid Id, Guid? ServiceId, Guid? CoverageRuleId, string ScopeKey, string TimeZoneId,
    DayOfWeek DayOfWeek, TimeOnly LocalStartTime, TimeOnly LocalEndTime, bool IsActive, string RowVersion);

public sealed record CourierSlaAdminDto(
    Guid Id, Guid? ServiceId, Guid? CoverageRuleId, string ScopeKey, int AcceptanceTargetMinutes,
    int PickupSchedulingTargetMinutes, int PickupCompletionTargetMinutes,
    int DeliveryToSchoolTargetMinutes, int? ReceiptConfirmationTargetMinutes,
    bool IsActive, string RowVersion);

public sealed record CourierHealthDto(
    IntegrationHealthStatus Status, string? SafeCode, DateTimeOffset CheckedAtUtc,
    bool IsAvailable, bool IsOperational);

public sealed record CourierAdminDetailDto(
    Guid IntegrationId, string ProviderCode, string DisplayNameAr, string? DisplayNameEn,
    bool IsActive, int ConfigurationVersion, string ConcurrencyRowVersion,
    CourierProviderProfileDto? Profile, IReadOnlyList<CourierServiceDto> Services,
    IReadOnlyList<CourierCoverageDto> Coverage, IReadOnlyList<CourierWindowDto> Windows,
    IReadOnlyList<CourierSlaAdminDto> Slas, IReadOnlyList<CourierHealthDto> HealthHistory);

public sealed record CourierWriteExpectation(string ExpectedRowVersion, int ExpectedConfigurationVersion);
public sealed record UpdateCourierProfileRequest(
    string DescriptionAr, string DescriptionEn, string? LogoReference, string TermsUrl,
    string PrivacyUrl, string ExpectedRowVersion, int ExpectedConfigurationVersion);
public sealed record UpsertCourierServiceRequest(
    string Code, string NameAr, string NameEn, string DescriptionAr, string DescriptionEn,
    int SortOrder, int MinimumPickupLeadTimeMinutes, TimeOnly DailyCutoffLocalTime,
    int MaximumFuturePickupDays, int AcceptanceWindowMinutes, bool SupportsScheduledPickup,
    bool SupportsSameDayPickup, bool CanCreatePickup, bool CanQueryStatus, bool SupportsWebhook,
    bool SupportsPolling, bool SupportsManualUpdates, bool SupportsCancellationBeforePickup,
    bool SupportsCourierAssignment, bool SupportsProofOfPickup, bool SupportsProofOfDelivery,
    bool SupportsDropOffPoint, int MaximumEnvelopeWeightGrams, decimal MaximumEnvelopeLengthCm,
    decimal MaximumEnvelopeWidthCm, decimal MaximumEnvelopeHeightCm,
    string ExpectedRowVersion, int ExpectedConfigurationVersion);
public sealed record UpsertCourierCoverageRequest(
    Guid? ServiceId, Guid CountryId, Guid? GovernorateId, Guid? CityId, Guid? DistrictId,
    CourierCoverageResult Result, string? NotesAr, string? NotesEn,
    string ExpectedRowVersion, int ExpectedConfigurationVersion);
public sealed record UpsertCourierWindowRequest(
    Guid? ServiceId, Guid? CoverageRuleId, string ScopeKey, string TimeZoneId, DayOfWeek DayOfWeek,
    TimeOnly LocalStartTime, TimeOnly LocalEndTime,
    string ExpectedRowVersion, int ExpectedConfigurationVersion);
public sealed record UpsertCourierSlaRequest(
    Guid? ServiceId, Guid? CoverageRuleId, string ScopeKey, int AcceptanceTargetMinutes,
    int PickupSchedulingTargetMinutes, int PickupCompletionTargetMinutes,
    int DeliveryToSchoolTargetMinutes, int? ReceiptConfirmationTargetMinutes,
    string ExpectedRowVersion, int ExpectedConfigurationVersion);
public sealed record SetCourierChildActiveRequest(
    bool IsActive, string ExpectedRowVersion, int ExpectedConfigurationVersion);

public interface ICourierAdministrationService
{
    Task<Result<CourierAdminDetailDto>> GetAsync(Guid integrationId, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> UpdateProfileAsync(Guid integrationId, UpdateCourierProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> CreateServiceAsync(Guid integrationId, UpsertCourierServiceRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> UpdateServiceAsync(Guid integrationId, Guid serviceId, UpsertCourierServiceRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> SetServiceActiveAsync(Guid integrationId, Guid serviceId, SetCourierChildActiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> CreateCoverageAsync(Guid integrationId, UpsertCourierCoverageRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> UpdateCoverageAsync(Guid integrationId, Guid coverageId, UpsertCourierCoverageRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> SetCoverageActiveAsync(Guid integrationId, Guid coverageId, SetCourierChildActiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> CreateWindowAsync(Guid integrationId, UpsertCourierWindowRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> UpdateWindowAsync(Guid integrationId, Guid windowId, UpsertCourierWindowRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> SetWindowActiveAsync(Guid integrationId, Guid windowId, SetCourierChildActiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> CreateSlaAsync(Guid integrationId, UpsertCourierSlaRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> UpdateSlaAsync(Guid integrationId, Guid slaId, UpsertCourierSlaRequest request, CancellationToken cancellationToken = default);
    Task<Result<CourierAdminDetailDto>> SetSlaActiveAsync(Guid integrationId, Guid slaId, SetCourierChildActiveRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CourierAvailabilityOptionDto>>> PreviewAsync(Guid integrationId, CourierLocation location, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<CourierHealthDto>>> HealthHistoryAsync(Guid integrationId, int take, CancellationToken cancellationToken = default);
}

public sealed record CourierAdmissionDestination(
    Guid ApplicationId, AdmissionApplicationStatus Status, bool BranchIsActive,
    string BranchNameAr, string? BranchNameEn, string? AddressAr, string? AddressEn,
    string CityNameAr, string? CityNameEn, string DistrictNameAr, string? DistrictNameEn);

public interface ICourierAdmissionContextReader
{
    Task<CourierAdmissionDestination?> GetOwnedDestinationAsync(
        Guid parentUserId, Guid applicationId, CancellationToken cancellationToken = default);
}

public sealed record CourierDestinationBranchDto(
    string NameAr, string? NameEn, string? AddressAr, string? AddressEn,
    string CityNameAr, string? CityNameEn, string DistrictNameAr, string? DistrictNameEn);
public sealed record ParentCourierAvailabilityDto(
    CourierDestinationBranchDto DestinationBranch,
    IReadOnlyList<CourierAvailabilityOptionDto> Options,
    IReadOnlyList<string> ReasonCodes);
