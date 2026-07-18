using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Couriers;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Couriers;

public sealed class CourierAdministrationService(
    SchooleraDbContext db,
    IUnitOfWork unitOfWork,
    ICourierLocationHierarchyValidator hierarchyValidator,
    ICourierAvailabilityResolver availabilityResolver) : ICourierAdministrationService
{
    public async Task<Result<CourierAdminDetailDto>> GetAsync(Guid integrationId, CancellationToken ct = default)
    {
        if (integrationId == Guid.Empty) return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
        var integration = await CourierIntegration(integrationId, false, ct);
        return integration is null ? Missing<CourierAdminDetailDto>() :
            Result<CourierAdminDetailDto>.Success(await MapDetail(integration, ct));
    }

    public async Task<Result<CourierAdminDetailDto>> UpdateProfileAsync(
        Guid id, UpdateCourierProfileRequest r, CancellationToken ct = default)
    {
        var integration = await CourierIntegration(id, true, ct);
        var profile = integration is null ? null :
            await db.CourierProviderProfiles.SingleOrDefaultAsync(x => x.IntegrationId == id, ct);
        if (integration is null) return Missing<CourierAdminDetailDto>();
        if (profile is null)
        {
            if (!Expect(integration, integration.RowVersion, 0, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            try
            {
                db.CourierProviderProfiles.Add(new CourierProviderProfile(
                    id, r.DescriptionAr, r.DescriptionEn, r.LogoReference, r.TermsUrl, r.PrivacyUrl));
            }
            catch (ArgumentException)
            {
                return Invalid<CourierAdminDetailDto>("courier.invalidProfile");
            }
            return await SaveAndDetail(integration, ct);
        }
        if (!Expect(profile, profile.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
            return Conflict<CourierAdminDetailDto>();
        try { profile.Update(r.DescriptionAr, r.DescriptionEn, r.LogoReference, r.TermsUrl, r.PrivacyUrl); }
        catch (ArgumentException) { return Invalid<CourierAdminDetailDto>("courier.invalidProfile"); }
        return await SaveAndDetail(integration, ct);
    }

    public Task<Result<CourierAdminDetailDto>> CreateServiceAsync(Guid id, UpsertCourierServiceRequest r, CancellationToken ct = default) =>
        ServiceMutation(id, null, r, false, ct);
    public Task<Result<CourierAdminDetailDto>> UpdateServiceAsync(Guid id, Guid childId, UpsertCourierServiceRequest r, CancellationToken ct = default) =>
        ServiceMutation(id, childId, r, false, ct);
    public async Task<Result<CourierAdminDetailDto>> SetServiceActiveAsync(Guid id, Guid childId, SetCourierChildActiveRequest r, CancellationToken ct = default)
    {
        var loaded = await LoadMutation<CourierService>(id, childId, r.ExpectedRowVersion, r.ExpectedConfigurationVersion, ct);
        if (loaded.Error is not null) return loaded.Error;
        loaded.Child!.SetActive(r.IsActive); loaded.Profile!.BumpConfigurationVersion();
        return await SaveAndDetail(loaded.Integration!, ct);
    }

    private async Task<Result<CourierAdminDetailDto>> ServiceMutation(
        Guid id, Guid? childId, UpsertCourierServiceRequest r, bool _, CancellationToken ct)
    {
        if (id == Guid.Empty || childId == Guid.Empty)
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
        if (r.SupportsDropOffPoint)
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.DropOffPointUnsupported);
        if (string.IsNullOrWhiteSpace(r.Code))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
        var integration = await CourierIntegration(id, true, ct);
        var profile = integration is null ? null : await db.CourierProviderProfiles.SingleOrDefaultAsync(x => x.IntegrationId == id, ct);
        if (integration is null || profile is null) return Missing<CourierAdminDetailDto>();
        CourierService? child = null;
        var token = r.ExpectedRowVersion;
        if (childId.HasValue)
        {
            child = await db.CourierServices.SingleOrDefaultAsync(x => x.Id == childId && x.IntegrationId == id, ct);
            if (child is null || !Expect(child, child.RowVersion, profile.ConfigurationVersion, token, r.ExpectedConfigurationVersion))
                return child is null ? Missing<CourierAdminDetailDto>() : Conflict<CourierAdminDetailDto>();
        }
        else if (!Expect(profile, profile.RowVersion, profile.ConfigurationVersion, token, r.ExpectedConfigurationVersion))
            return Conflict<CourierAdminDetailDto>();
        if (await db.CourierServices.AnyAsync(x => x.IntegrationId == id && x.Code == r.Code.Trim().ToUpper() && x.Id != childId, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.DuplicateActiveScope);
        try
        {
            if (child is null)
            {
                child = NewService(id, r);
                db.CourierServices.Add(child);
            }
            else UpdateService(child, r);
            profile.BumpConfigurationVersion();
        }
        catch (ArgumentException) { return Invalid<CourierAdminDetailDto>("courier.invalidService"); }
        return await SaveAndDetail(integration, ct);
    }

    public Task<Result<CourierAdminDetailDto>> CreateCoverageAsync(Guid id, UpsertCourierCoverageRequest r, CancellationToken ct = default) =>
        CoverageMutation(id, null, r, ct);
    public Task<Result<CourierAdminDetailDto>> UpdateCoverageAsync(Guid id, Guid childId, UpsertCourierCoverageRequest r, CancellationToken ct = default) =>
        CoverageMutation(id, childId, r, ct);
    public async Task<Result<CourierAdminDetailDto>> SetCoverageActiveAsync(Guid id, Guid childId, SetCourierChildActiveRequest r, CancellationToken ct = default)
    {
        var loaded = await LoadMutation<CourierCoverageRule>(id, childId, r.ExpectedRowVersion, r.ExpectedConfigurationVersion, ct);
        if (loaded.Error is not null) return loaded.Error;
        if (r.IsActive && await db.CourierCoverageRules.AnyAsync(x =>
                x.IntegrationId == id && x.Id != childId && x.IsActive &&
                x.ServiceId == loaded.Child!.ServiceId && x.ScopeKey == loaded.Child.ScopeKey, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.DuplicateActiveScope);
        loaded.Child!.SetActive(r.IsActive); loaded.Profile!.BumpConfigurationVersion();
        return await SaveAndDetail(loaded.Integration!, ct);
    }

    private async Task<Result<CourierAdminDetailDto>> CoverageMutation(Guid id, Guid? childId, UpsertCourierCoverageRequest r, CancellationToken ct)
    {
        if (id == Guid.Empty || childId == Guid.Empty || r.CountryId == Guid.Empty)
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
        var location = await hierarchyValidator.ValidateAsync(new(r.CountryId, r.GovernorateId, r.CityId, r.DistrictId), ct);
        if (location is null) return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidHierarchy);
        if (r.ServiceId.HasValue && !await db.CourierServices.AnyAsync(x => x.Id == r.ServiceId && x.IntegrationId == id, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidOwnership);
        var integration = await CourierIntegration(id, true, ct);
        var profile = integration is null ? null : await db.CourierProviderProfiles.SingleOrDefaultAsync(x => x.IntegrationId == id, ct);
        if (integration is null || profile is null) return Missing<CourierAdminDetailDto>();
        CourierCoverageRule? child = null;
        if (childId.HasValue)
        {
            child = await db.CourierCoverageRules.SingleOrDefaultAsync(x => x.Id == childId && x.IntegrationId == id, ct);
            if (child is null) return Missing<CourierAdminDetailDto>();
            if (child.ServiceId != r.ServiceId || child.CountryId != r.CountryId || child.GovernorateId != r.GovernorateId ||
                child.CityId != r.CityId || child.DistrictId != r.DistrictId)
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidOwnership);
            if (!Expect(child, child.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            try { child.Update(r.Result, r.NotesAr, r.NotesEn); }
            catch (ArgumentException) { return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest); }
        }
        else
        {
            if (!Expect(profile, profile.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            var scope = CourierScopeKeys.Geography(r.CountryId, r.GovernorateId, r.CityId, r.DistrictId);
            if (await db.CourierCoverageRules.AnyAsync(x => x.IntegrationId == id && x.IsActive && x.ServiceId == r.ServiceId && x.ScopeKey == scope, ct))
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.DuplicateActiveScope);
            try
            {
                db.CourierCoverageRules.Add(new(id, r.ServiceId, r.CountryId, r.GovernorateId, r.CityId, r.DistrictId, r.Result, r.NotesAr, r.NotesEn));
            }
            catch (ArgumentException)
            {
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
            }
        }
        profile.BumpConfigurationVersion();
        return await SaveAndDetail(integration, ct);
    }

    public Task<Result<CourierAdminDetailDto>> CreateWindowAsync(Guid id, UpsertCourierWindowRequest r, CancellationToken ct = default) =>
        WindowMutation(id, null, r, ct);
    public Task<Result<CourierAdminDetailDto>> UpdateWindowAsync(Guid id, Guid childId, UpsertCourierWindowRequest r, CancellationToken ct = default) =>
        WindowMutation(id, childId, r, ct);
    public async Task<Result<CourierAdminDetailDto>> SetWindowActiveAsync(Guid id, Guid childId, SetCourierChildActiveRequest r, CancellationToken ct = default)
    {
        var loaded = await LoadMutation<CourierOperatingWindow>(id, childId, r.ExpectedRowVersion, r.ExpectedConfigurationVersion, ct);
        if (loaded.Error is not null) return loaded.Error;
        if (r.IsActive && await HasOverlap(id, loaded.Child!.ServiceId, loaded.Child.ScopeKey, loaded.Child.DayOfWeek,
                loaded.Child.LocalStartTime, loaded.Child.LocalEndTime, childId, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.OverlappingWindow);
        loaded.Child!.SetActive(r.IsActive); loaded.Profile!.BumpConfigurationVersion();
        return await SaveAndDetail(loaded.Integration!, ct);
    }

    private async Task<Result<CourierAdminDetailDto>> WindowMutation(Guid id, Guid? childId, UpsertCourierWindowRequest r, CancellationToken ct)
    {
        if (id == Guid.Empty || childId == Guid.Empty)
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
        if (!ValidZone(r.TimeZoneId)) return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidTimeZone);
        if (r.LocalEndTime <= r.LocalStartTime) return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidWindow);
        if (!await OwnedReferences(id, r.ServiceId, r.CoverageRuleId, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidOwnership);
        var scopeKey = await ResolveScopeKey(id, r.ServiceId, r.CoverageRuleId, r.ScopeKey, ct);
        if (scopeKey is null) return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidScope);
        if (await HasOverlap(id, r.ServiceId, scopeKey, r.DayOfWeek, r.LocalStartTime, r.LocalEndTime, childId, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.OverlappingWindow);
        var integration = await CourierIntegration(id, true, ct);
        var profile = integration is null ? null : await db.CourierProviderProfiles.SingleOrDefaultAsync(x => x.IntegrationId == id, ct);
        if (integration is null || profile is null) return Missing<CourierAdminDetailDto>();
        if (childId.HasValue)
        {
            var child = await db.CourierOperatingWindows.SingleOrDefaultAsync(x => x.Id == childId && x.IntegrationId == id, ct);
            if (child is null) return Missing<CourierAdminDetailDto>();
            if (child.ServiceId != r.ServiceId || child.CoverageRuleId != r.CoverageRuleId ||
                !child.ScopeKey.Equals(scopeKey, StringComparison.OrdinalIgnoreCase))
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidOwnership);
            if (!Expect(child, child.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            child.Update(r.TimeZoneId, r.DayOfWeek, r.LocalStartTime, r.LocalEndTime);
        }
        else
        {
            if (!Expect(profile, profile.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            try
            {
                db.CourierOperatingWindows.Add(new(id, r.ServiceId, r.CoverageRuleId, scopeKey, r.TimeZoneId, r.DayOfWeek, r.LocalStartTime, r.LocalEndTime));
            }
            catch (ArgumentException)
            {
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
            }
        }
        profile.BumpConfigurationVersion();
        return await SaveAndDetail(integration, ct);
    }

    public Task<Result<CourierAdminDetailDto>> CreateSlaAsync(Guid id, UpsertCourierSlaRequest r, CancellationToken ct = default) =>
        SlaMutation(id, null, r, ct);
    public Task<Result<CourierAdminDetailDto>> UpdateSlaAsync(Guid id, Guid childId, UpsertCourierSlaRequest r, CancellationToken ct = default) =>
        SlaMutation(id, childId, r, ct);
    public async Task<Result<CourierAdminDetailDto>> SetSlaActiveAsync(Guid id, Guid childId, SetCourierChildActiveRequest r, CancellationToken ct = default)
    {
        var loaded = await LoadMutation<CourierSlaDefinition>(id, childId, r.ExpectedRowVersion, r.ExpectedConfigurationVersion, ct);
        if (loaded.Error is not null) return loaded.Error;
        if (r.IsActive && await db.CourierSlaDefinitions.AnyAsync(x => x.IntegrationId == id && x.Id != childId &&
                x.IsActive && x.ServiceId == loaded.Child!.ServiceId && x.ScopeKey == loaded.Child.ScopeKey, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.DuplicateActiveScope);
        loaded.Child!.SetActive(r.IsActive); loaded.Profile!.BumpConfigurationVersion();
        return await SaveAndDetail(loaded.Integration!, ct);
    }

    private async Task<Result<CourierAdminDetailDto>> SlaMutation(Guid id, Guid? childId, UpsertCourierSlaRequest r, CancellationToken ct)
    {
        if (id == Guid.Empty || childId == Guid.Empty)
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest);
        if (r.AcceptanceTargetMinutes <= 0 || r.PickupSchedulingTargetMinutes <= 0 ||
            r.PickupCompletionTargetMinutes <= 0 || r.DeliveryToSchoolTargetMinutes <= 0 ||
            r.ReceiptConfirmationTargetMinutes is <= 0)
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidSla);
        if (!await OwnedReferences(id, r.ServiceId, r.CoverageRuleId, ct))
            return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidOwnership);
        var scopeKey = await ResolveScopeKey(id, r.ServiceId, r.CoverageRuleId, r.ScopeKey, ct);
        if (scopeKey is null) return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidScope);
        var integration = await CourierIntegration(id, true, ct);
        var profile = integration is null ? null : await db.CourierProviderProfiles.SingleOrDefaultAsync(x => x.IntegrationId == id, ct);
        if (integration is null || profile is null) return Missing<CourierAdminDetailDto>();
        if (childId.HasValue)
        {
            var child = await db.CourierSlaDefinitions.SingleOrDefaultAsync(x => x.Id == childId && x.IntegrationId == id, ct);
            if (child is null) return Missing<CourierAdminDetailDto>();
            if (child.ServiceId != r.ServiceId || child.CoverageRuleId != r.CoverageRuleId ||
                !child.ScopeKey.Equals(scopeKey, StringComparison.OrdinalIgnoreCase))
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidOwnership);
            if (!Expect(child, child.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            try
            {
                child.Update(r.AcceptanceTargetMinutes, r.PickupSchedulingTargetMinutes, r.PickupCompletionTargetMinutes,
                    r.DeliveryToSchoolTargetMinutes, r.ReceiptConfirmationTargetMinutes);
            }
            catch (ArgumentException)
            {
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidSla);
            }
        }
        else
        {
            if (!Expect(profile, profile.RowVersion, profile.ConfigurationVersion, r.ExpectedRowVersion, r.ExpectedConfigurationVersion))
                return Conflict<CourierAdminDetailDto>();
            if (await db.CourierSlaDefinitions.AnyAsync(x => x.IntegrationId == id && x.IsActive && x.ServiceId == r.ServiceId && x.ScopeKey == scopeKey, ct))
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.DuplicateActiveScope);
            try
            {
                db.CourierSlaDefinitions.Add(new(id, r.ServiceId, r.CoverageRuleId, scopeKey,
                    r.AcceptanceTargetMinutes, r.PickupSchedulingTargetMinutes, r.PickupCompletionTargetMinutes,
                    r.DeliveryToSchoolTargetMinutes, r.ReceiptConfirmationTargetMinutes));
            }
            catch (ArgumentException)
            {
                return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidSla);
            }
        }
        profile.BumpConfigurationVersion();
        return await SaveAndDetail(integration, ct);
    }

    public async Task<Result<IReadOnlyList<CourierAvailabilityOptionDto>>> PreviewAsync(
        Guid integrationId, CourierLocation location, CancellationToken ct = default)
    {
        var integration = await CourierIntegration(integrationId, false, ct);
        if (integration is null) return Missing<IReadOnlyList<CourierAvailabilityOptionDto>>();
        if (!integration.IsActive || !integration.IsDefault)
            return Invalid<IReadOnlyList<CourierAvailabilityOptionDto>>("courier.previewRequiresActiveDefault");
        return await availabilityResolver.ResolveAsync(new(location, DateTimeOffset.UtcNow), ct);
    }

    public async Task<Result<IReadOnlyList<CourierHealthDto>>> HealthHistoryAsync(Guid id, int take, CancellationToken ct = default)
    {
        if (await CourierIntegration(id, false, ct) is null) return Missing<IReadOnlyList<CourierHealthDto>>();
        var rows = await db.CourierHealthCheckRecords.AsNoTracking().Where(x => x.IntegrationId == id)
            .OrderByDescending(x => x.CheckedAtUtc).Take(Math.Clamp(take, 1, 200))
            .Select(x => new CourierHealthDto(x.Status, x.SafeCode, x.CheckedAtUtc, x.IsAvailable, x.IsOperational))
            .ToListAsync(ct);
        return Result<IReadOnlyList<CourierHealthDto>>.Success(rows);
    }

    private async Task<(PlatformIntegrationConfiguration? Integration, CourierProviderProfile? Profile, T? Child, Result<CourierAdminDetailDto>? Error)>
        LoadMutation<T>(Guid id, Guid childId, string rowVersion, int version, CancellationToken ct) where T : class
    {
        var integration = await CourierIntegration(id, true, ct);
        var profile = integration is null ? null : await db.CourierProviderProfiles.SingleOrDefaultAsync(x => x.IntegrationId == id, ct);
        var child = integration is null ? null : await db.Set<T>().FindAsync([childId], ct);
        var integrationId = child?.GetType().GetProperty("IntegrationId")?.GetValue(child) as Guid?;
        if (integration is null || profile is null || child is null || integrationId != id)
            return (integration, profile, child, Missing<CourierAdminDetailDto>());
        var current = (byte[]?)child.GetType().GetProperty("RowVersion")?.GetValue(child) ?? [];
        if (!Expect(child, current, profile.ConfigurationVersion, rowVersion, version))
            return (integration, profile, child, Conflict<CourierAdminDetailDto>());
        return (integration, profile, child, null);
    }

    private bool Expect(object entity, byte[] current, int currentVersion, string supplied, int suppliedVersion)
    {
        byte[] token;
        try { token = Convert.FromBase64String(supplied); } catch { return false; }
        if (currentVersion != suppliedVersion || !current.AsSpan().SequenceEqual(token)) return false;
        db.Entry(entity).Property("RowVersion").OriginalValue = token;
        return true;
    }

    private Task<PlatformIntegrationConfiguration?> CourierIntegration(Guid id, bool tracked, CancellationToken ct)
    {
        var query = db.PlatformIntegrationConfigurations.Where(x => x.Id == id && x.IntegrationType == IntegrationType.Courier);
        return (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(ct);
    }

    private async Task<Result<CourierAdminDetailDto>> SaveAndDetail(PlatformIntegrationConfiguration integration, CancellationToken ct)
    {
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (ConcurrencyConflictException) { return Conflict<CourierAdminDetailDto>(); }
        catch (DbUpdateException) { return Invalid<CourierAdminDetailDto>(CourierErrorCodes.InvalidRequest); }
        return Result<CourierAdminDetailDto>.Success(await MapDetail(integration, ct));
    }

    private async Task<CourierAdminDetailDto> MapDetail(PlatformIntegrationConfiguration integration, CancellationToken ct)
    {
        var profile = await db.CourierProviderProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.IntegrationId == integration.Id, ct);
        var services = await db.CourierServices.AsNoTracking().Where(x => x.IntegrationId == integration.Id).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var coverage = await db.CourierCoverageRules.AsNoTracking().Where(x => x.IntegrationId == integration.Id).ToListAsync(ct);
        var windows = await db.CourierOperatingWindows.AsNoTracking().Where(x => x.IntegrationId == integration.Id).ToListAsync(ct);
        var slas = await db.CourierSlaDefinitions.AsNoTracking().Where(x => x.IntegrationId == integration.Id).ToListAsync(ct);
        var health = await db.CourierHealthCheckRecords.AsNoTracking().Where(x => x.IntegrationId == integration.Id)
            .OrderByDescending(x => x.CheckedAtUtc).Take(20).ToListAsync(ct);
        return new(integration.Id, integration.ProviderCode, integration.DisplayNameAr, integration.DisplayNameEn, integration.IsActive,
            profile?.ConfigurationVersion ?? 0, B64(profile?.RowVersion ?? integration.RowVersion),
            profile is null ? null : new(profile.DescriptionAr, profile.DescriptionEn, profile.LogoReference, profile.TermsUrl, profile.PrivacyUrl,
                profile.ConfigurationVersion, B64(profile.RowVersion)),
            services.Select(x => new CourierServiceDto(x.Id, x.ServiceType, x.Code, x.NameAr, x.NameEn, x.DescriptionAr, x.DescriptionEn, x.IsActive,
                x.SortOrder, x.MinimumPickupLeadTimeMinutes, x.DailyCutoffLocalTime, x.MaximumFuturePickupDays, x.AcceptanceWindowMinutes,
                x.SupportsScheduledPickup, x.SupportsSameDayPickup, x.CanCreatePickup, x.CanQueryStatus, x.SupportsWebhook, x.SupportsPolling,
                x.SupportsManualUpdates, x.SupportsCancellationBeforePickup, x.SupportsCourierAssignment, x.SupportsProofOfPickup,
                x.SupportsProofOfDelivery, x.SupportsDropOffPoint, x.MaximumEnvelopeWeightGrams, x.MaximumEnvelopeLengthCm,
                x.MaximumEnvelopeWidthCm, x.MaximumEnvelopeHeightCm, B64(x.RowVersion))).ToArray(),
            coverage.Select(x => new CourierCoverageDto(x.Id, x.ServiceId, x.CountryId, x.GovernorateId, x.CityId, x.DistrictId,
                x.Result, x.NotesAr, x.NotesEn, x.IsActive, B64(x.RowVersion))).ToArray(),
            windows.Select(x => new CourierWindowDto(x.Id, x.ServiceId, x.CoverageRuleId, x.ScopeKey, x.TimeZoneId,
                x.DayOfWeek, x.LocalStartTime, x.LocalEndTime, x.IsActive, B64(x.RowVersion))).ToArray(),
            slas.Select(x => new CourierSlaAdminDto(x.Id, x.ServiceId, x.CoverageRuleId, x.ScopeKey, x.AcceptanceTargetMinutes,
                x.PickupSchedulingTargetMinutes, x.PickupCompletionTargetMinutes, x.DeliveryToSchoolTargetMinutes,
                x.ReceiptConfirmationTargetMinutes, x.IsActive, B64(x.RowVersion))).ToArray(),
            health.Select(x => new CourierHealthDto(x.Status, x.SafeCode, x.CheckedAtUtc, x.IsAvailable, x.IsOperational)).ToArray());
    }

    private async Task<bool> OwnedReferences(Guid id, Guid? serviceId, Guid? coverageId, CancellationToken ct) =>
        (!serviceId.HasValue || await db.CourierServices.AnyAsync(x => x.Id == serviceId && x.IntegrationId == id, ct)) &&
        (!coverageId.HasValue || await db.CourierCoverageRules.AnyAsync(x => x.Id == coverageId && x.IntegrationId == id, ct));
    private async Task<string?> ResolveScopeKey(
        Guid id, Guid? serviceId, Guid? coverageId, string? supplied, CancellationToken ct)
    {
        var normalized = string.IsNullOrWhiteSpace(supplied) ? null : supplied.Trim().ToLowerInvariant();
        if (coverageId.HasValue)
        {
            var coverage = await db.CourierCoverageRules.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == coverageId && x.IntegrationId == id, ct);
            if (coverage is null ||
                coverage.ServiceId.HasValue && coverage.ServiceId != serviceId ||
                normalized is not null && !string.Equals(normalized, coverage.ScopeKey, StringComparison.Ordinal))
                return null;
            return coverage.ScopeKey;
        }

        if (normalized is null) return null;
        return await db.CourierCoverageRules.AsNoTracking()
            .Where(x => x.IntegrationId == id && x.ScopeKey == normalized &&
                        (!x.ServiceId.HasValue || x.ServiceId == serviceId))
            .Select(x => x.ScopeKey)
            .FirstOrDefaultAsync(ct);
    }
    private Task<bool> HasOverlap(Guid id, Guid? serviceId, string scope, DayOfWeek day, TimeOnly start, TimeOnly end, Guid? except, CancellationToken ct) =>
        db.CourierOperatingWindows.AnyAsync(x => x.IntegrationId == id && x.IsActive && x.Id != except &&
            x.ServiceId == serviceId && x.ScopeKey == scope && x.DayOfWeek == day &&
            x.LocalStartTime < end && start < x.LocalEndTime, ct);
    private static bool ValidZone(string id) { try { _ = TimeZoneInfo.FindSystemTimeZoneById(id); return true; } catch { return false; } }
    private static string B64(byte[] value) => Convert.ToBase64String(value);
    private static Result<T> Missing<T>() => Result<T>.Failure(["Courier integration or child was not found."], [CourierErrorCodes.NotFound]);
    private static Result<T> Conflict<T>() => Result<T>.Failure(["Courier configuration was modified."], [CourierErrorCodes.Conflict]);
    private static Result<T> Invalid<T>(string code) => Result<T>.Failure(["Courier configuration is invalid."], [code]);

    private static CourierService NewService(Guid id, UpsertCourierServiceRequest r) => new(
        id, r.Code, r.NameAr, r.NameEn, r.DescriptionAr, r.DescriptionEn, r.SortOrder,
        r.MinimumPickupLeadTimeMinutes, r.DailyCutoffLocalTime, r.MaximumFuturePickupDays, r.AcceptanceWindowMinutes,
        r.SupportsScheduledPickup, r.SupportsSameDayPickup, r.CanCreatePickup, r.CanQueryStatus, r.SupportsWebhook,
        r.SupportsPolling, r.SupportsManualUpdates, r.SupportsCancellationBeforePickup, r.SupportsCourierAssignment,
        r.SupportsProofOfPickup, r.SupportsProofOfDelivery, r.SupportsDropOffPoint, r.MaximumEnvelopeWeightGrams,
        r.MaximumEnvelopeLengthCm, r.MaximumEnvelopeWidthCm, r.MaximumEnvelopeHeightCm);
    private static void UpdateService(CourierService x, UpsertCourierServiceRequest r) => x.Update(
        r.Code, r.NameAr, r.NameEn, r.DescriptionAr, r.DescriptionEn, r.SortOrder,
        r.MinimumPickupLeadTimeMinutes, r.DailyCutoffLocalTime, r.MaximumFuturePickupDays, r.AcceptanceWindowMinutes,
        r.SupportsScheduledPickup, r.SupportsSameDayPickup, r.CanCreatePickup, r.CanQueryStatus, r.SupportsWebhook,
        r.SupportsPolling, r.SupportsManualUpdates, r.SupportsCancellationBeforePickup, r.SupportsCourierAssignment,
        r.SupportsProofOfPickup, r.SupportsProofOfDelivery, r.SupportsDropOffPoint, r.MaximumEnvelopeWeightGrams,
        r.MaximumEnvelopeLengthCm, r.MaximumEnvelopeWidthCm, r.MaximumEnvelopeHeightCm);
}

public sealed class CourierAdmissionContextReader(SchooleraDbContext db) : ICourierAdmissionContextReader
{
    public Task<CourierAdmissionDestination?> GetOwnedDestinationAsync(Guid parentUserId, Guid applicationId, CancellationToken ct = default) =>
        db.AdmissionApplications.AsNoTracking()
            .Where(x => x.Id == applicationId && x.ParentUserId == parentUserId)
            .Select(x => new CourierAdmissionDestination(
                x.Id, x.Status, x.SchoolBranch.IsActive, x.SchoolBranch.NameAr, x.SchoolBranch.NameEn,
                x.SchoolBranch.AddressLineAr, x.SchoolBranch.AddressLineEn,
                x.SchoolBranch.City.NameAr, x.SchoolBranch.City.NameEn,
                x.SchoolBranch.District.NameAr, x.SchoolBranch.District.NameEn))
            .SingleOrDefaultAsync(ct);
}
