using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Schools.Fees;
using Schoolera.Application.Schools.Map;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class SchoolReadRepository(
    SchooleraDbContext dbContext,
    ICurrentUser currentUser,
    IFavoriteSchoolRepository favoriteSchoolRepository) : ISchoolReadRepository, ISchoolMapPinReadRepository
{
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);

    public Task<School?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        BuildDetailedQuery()
            .FirstOrDefaultAsync(
                school => school.Slug == slug && school.Status == SchoolStatus.Published,
                cancellationToken);

    public async Task<IReadOnlyList<PublicSchoolSearchProjection>?> GetRelatedPublishedAsync(
        string slug,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var current = await dbContext.Schools
            .AsNoTracking()
            .Where(school => school.Slug == slug && school.Status == SchoolStatus.Published)
            .Select(school => new
            {
                school.Id,
                MainDistrictId = school.Branches
                    .Where(branch => branch.IsActive)
                    .OrderByDescending(branch => branch.IsMainBranch)
                    .Select(branch => (Guid?)branch.DistrictId)
                    .FirstOrDefault(),
                MainCityId = school.Branches
                    .Where(branch => branch.IsActive)
                    .OrderByDescending(branch => branch.IsMainBranch)
                    .Select(branch => (Guid?)branch.CityId)
                    .FirstOrDefault(),
                Latitude = school.Branches
                    .Where(branch => branch.IsActive && branch.Latitude != null && branch.Longitude != null)
                    .OrderByDescending(branch => branch.IsMainBranch)
                    .Select(branch => (double?)branch.Latitude)
                    .FirstOrDefault(),
                Longitude = school.Branches
                    .Where(branch => branch.IsActive && branch.Latitude != null && branch.Longitude != null)
                    .OrderByDescending(branch => branch.IsMainBranch)
                    .Select(branch => (double?)branch.Longitude)
                    .FirstOrDefault(),
                CurriculumIds = school.Curricula
                    .Where(link => link.Curriculum.IsActive)
                    .Select(link => link.CurriculumId)
                    .ToArray(),
                StageIds = school.Branches
                    .Where(branch => branch.IsActive)
                    .SelectMany(branch => branch.StageOfferings)
                    .Where(offering => offering.IsActive)
                    .Select(offering => offering.EducationalStageId)
                    .Distinct()
                    .ToArray(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return null;
        }

        var districtId = current.MainDistrictId;
        var cityId = current.MainCityId;
        var curriculumIds = current.CurriculumIds;
        var stageIds = current.StageIds;
        var hasOriginCoordinates = current.Latitude is not null && current.Longitude is not null;
        var originLat = current.Latitude ?? 0d;
        var originLon = current.Longitude ?? 0d;

        var ranked = await dbContext.Schools
            .AsNoTracking()
            .Where(school => school.Status == SchoolStatus.Published && school.Id != current.Id)
            .Select(school => new
            {
                school.Id,
                school.NameAr,
                school.CreatedAtUtc,
                SameDistrict = districtId != null && school.Branches.Any(branch =>
                    branch.IsActive && branch.DistrictId == districtId),
                SameCity = cityId != null && school.Branches.Any(branch =>
                    branch.IsActive && branch.CityId == cityId),
                SharedStage = stageIds.Length > 0 && school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive && stageIds.Contains(offering.EducationalStageId))),
                SharedCurriculum = curriculumIds.Length > 0 && school.Curricula.Any(link =>
                    link.Curriculum.IsActive && curriculumIds.Contains(link.CurriculumId)),
                Latitude = school.Branches
                    .Where(branch => branch.IsActive && branch.Latitude != null && branch.Longitude != null)
                    .OrderByDescending(branch => branch.IsMainBranch)
                    .Select(branch => (double?)branch.Latitude)
                    .FirstOrDefault(),
                Longitude = school.Branches
                    .Where(branch => branch.IsActive && branch.Latitude != null && branch.Longitude != null)
                    .OrderByDescending(branch => branch.IsMainBranch)
                    .Select(branch => (double?)branch.Longitude)
                    .FirstOrDefault(),
            })
            .OrderByDescending(row => row.SameDistrict)
            .ThenByDescending(row => row.SameCity)
            .ThenByDescending(row => row.SharedStage)
            .ThenByDescending(row => row.SharedCurriculum)
            .ThenBy(row => row.NameAr)
            .ThenByDescending(row => row.CreatedAtUtc)
            .ThenBy(row => row.Id)
            .Take(Math.Clamp(limit, 1, 8) * 3)
            .ToListAsync(cancellationToken);

        // Optional geographic refinement only when both schools have coordinates (still SQL-selected above).
        var orderedIds = ranked
            .OrderByDescending(row => row.SameDistrict)
            .ThenByDescending(row => row.SameCity)
            .ThenByDescending(row => row.SharedStage)
            .ThenByDescending(row => row.SharedCurriculum)
            .ThenBy(row =>
            {
                if (!hasOriginCoordinates || row.Latitude is null || row.Longitude is null)
                {
                    return double.MaxValue;
                }

                return ApproximateDistanceKm(originLat, originLon, row.Latitude.Value, row.Longitude.Value);
            })
            .ThenBy(row => row.NameAr, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(row => row.CreatedAtUtc)
            .ThenBy(row => row.Id)
            .Select(row => row.Id)
            .Distinct()
            .Take(Math.Clamp(limit, 1, 8))
            .ToArray();

        if (orderedIds.Length == 0)
        {
            return Array.Empty<PublicSchoolSearchProjection>();
        }

        return await BuildProjectionsAsync(
            orderedIds,
            new PublicSchoolListFilter(),
            academicYearId: null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PublicSchoolSearchProjection>> GetPublishedListCardsByIdsAsync(
        IReadOnlyList<Guid> orderedIds,
        CancellationToken cancellationToken = default)
    {
        if (orderedIds.Count == 0)
        {
            return Array.Empty<PublicSchoolSearchProjection>();
        }

        var publishedIds = await dbContext.Schools.AsNoTracking()
            .Where(school =>
                orderedIds.Contains(school.Id) &&
                school.Status == SchoolStatus.Published)
            .Select(school => school.Id)
            .ToListAsync(cancellationToken);

        var orderedPublished = orderedIds.Where(publishedIds.Contains).ToArray();
        if (orderedPublished.Length == 0)
        {
            return Array.Empty<PublicSchoolSearchProjection>();
        }

        return await BuildProjectionsAsync(
            orderedPublished,
            new PublicSchoolListFilter(),
            academicYearId: null,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<PublicSchoolMapPinProjection> Pins, bool IsTruncated)> SearchMapPinsAsync(
        PublicSchoolMapPinsFilter filter,
        CancellationToken cancellationToken = default)
    {
        var academicYearId = await ResolveAcademicYearIdAsync(filter.SchoolFilter.AcademicYearId, cancellationToken);
        var searchTerm = NormalizeSearch(filter.SchoolFilter.Search);

        IQueryable<School> schools = dbContext.Schools
            .AsNoTracking()
            .Where(school => school.Status == SchoolStatus.Published);

        schools = ApplyPublicFilter(schools, filter.SchoolFilter, searchTerm, academicYearId);

        // Apply non-geographic filters first; then project matching active branches with coordinates.
        var branchQuery = schools
            .SelectMany(school => school.Branches
                .Where(branch =>
                    branch.IsActive &&
                    branch.Latitude != null &&
                    branch.Longitude != null)
                .Select(branch => new
                {
                    SchoolId = school.Id,
                    SchoolSlug = school.Slug,
                    BranchId = branch.Id,
                    SchoolNameAr = school.NameAr,
                    SchoolNameEn = school.NameEn,
                    BranchNameAr = branch.NameAr,
                    BranchNameEn = branch.NameEn,
                    Latitude = (double)branch.Latitude!,
                    Longitude = (double)branch.Longitude!,
                    school.LogoUrl,
                    CityNameAr = branch.City.NameAr,
                    CityNameEn = branch.City.NameEn,
                    DistrictNameAr = branch.District.NameAr,
                    DistrictNameEn = branch.District.NameEn,
                    school.FeeVisibilityPolicy,
                    IsAdmissionOpen = branch.StageOfferings.Any(offering =>
                        offering.IsActive && offering.IsAdmissionOpen),
                    MinFee = branch.TuitionFees
                        .Where(fee =>
                            fee.IsActive &&
                            fee.IsPublished &&
                            fee.CurrencyCode == "EGP" &&
                            (!academicYearId.HasValue || fee.AcademicYearId == academicYearId))
                        .Select(fee => (decimal?)fee.Amount)
                        .Min(),
                }));

        if (filter.GeoMode == MapGeoSearchMode.BoundingBox)
        {
            var north = filter.NorthLatitude!.Value;
            var south = filter.SouthLatitude!.Value;
            var east = filter.EastLongitude!.Value;
            var west = filter.WestLongitude!.Value;
            branchQuery = branchQuery.Where(pin =>
                pin.Latitude <= north &&
                pin.Latitude >= south &&
                pin.Longitude <= east &&
                pin.Longitude >= west);
        }
        else
        {
            // Coarse SQL-translatable rectangle around the radius, then precise filter in memory.
            var centerLat = filter.CenterLatitude!.Value;
            var centerLon = filter.CenterLongitude!.Value;
            var radiusKm = filter.RadiusKm!.Value;
            var latDelta = radiusKm / 111.0d;
            var cos = Math.Cos(centerLat * Math.PI / 180d);
            var lonDelta = radiusKm / (111.0d * Math.Max(0.2d, Math.Abs(cos)));
            var north = centerLat + latDelta;
            var south = centerLat - latDelta;
            var east = centerLon + lonDelta;
            var west = centerLon - lonDelta;
            branchQuery = branchQuery.Where(pin =>
                pin.Latitude <= north &&
                pin.Latitude >= south &&
                pin.Longitude <= east &&
                pin.Longitude >= west);
        }

        var take = filter.GeoMode == MapGeoSearchMode.Radius
            ? MapSearchLimits.AbsoluteMaxPins + 1
            : filter.MaxPins + 1;
        var rows = await branchQuery
            .OrderBy(pin => pin.SchoolNameAr)
            .ThenBy(pin => pin.BranchNameAr)
            .ThenBy(pin => pin.BranchId)
            .Take(take)
            .ToListAsync(cancellationToken);

        var isParent = currentUser.IsAuthenticated && currentUser.IsInRole(SchooleraRoles.Parent);

        var withDistance = rows.Select(row =>
        {
            double? distance = null;
            if (filter.CenterLatitude is { } cLat && filter.CenterLongitude is { } cLon)
            {
                distance = ApproximateDistanceKm(cLat, cLon, row.Latitude, row.Longitude);
            }

            return (Row: row, Distance: distance);
        });

        if (filter.GeoMode == MapGeoSearchMode.Radius && filter.RadiusKm is { } radius)
        {
            withDistance = withDistance
                .Where(item => item.Distance is not null && item.Distance <= radius)
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.Row.SchoolNameAr, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Row.BranchNameAr, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Row.BranchId);
        }

        var ranked = withDistance.ToList();
        var truncated = ranked.Count > filter.MaxPins;
        var selected = ranked.Take(filter.MaxPins);

        var pins = selected.Select(item =>
        {
            var row = item.Row;
            var hasPublishedFees = row.MinFee is not null;
            var canReveal = FeeVisibilityResolver.CanRevealDetailedFees(row.FeeVisibilityPolicy, isParent);
            var feesRequireLogin =
                FeeVisibilityResolver.FeesRequireLogin(row.FeeVisibilityPolicy) && !canReveal;

            return new PublicSchoolMapPinProjection(
                row.SchoolId,
                row.SchoolSlug,
                row.BranchId,
                row.SchoolNameAr,
                row.SchoolNameEn,
                row.BranchNameAr,
                row.BranchNameEn,
                row.Latitude,
                row.Longitude,
                row.LogoUrl,
                row.CityNameAr,
                row.CityNameEn,
                row.DistrictNameAr,
                row.DistrictNameEn,
                row.IsAdmissionOpen,
                canReveal ? row.MinFee : null,
                canReveal && hasPublishedFees ? "EGP" : null,
                hasPublishedFees,
                feesRequireLogin,
                item.Distance is double d ? Math.Round(d, 1) : null);
        }).ToArray();

        return (pins, truncated);
    }

    public async Task<(IReadOnlyList<PublicSchoolSearchProjection> Items, int TotalCount)> SearchPublishedAsync(
        PagedRequest paging,
        PublicSchoolListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var academicYearId = await ResolveAcademicYearIdAsync(filter.AcademicYearId, cancellationToken);
        var searchTerm = NormalizeSearch(filter.Search);

        IQueryable<School> schools = dbContext.Schools
            .AsNoTracking()
            .Where(school => school.Status == SchoolStatus.Published);

        schools = ApplyPublicFilter(schools, filter, searchTerm, academicYearId);

        var totalCount = await schools.CountAsync(cancellationToken);
        if (totalCount == 0)
        {
            return ([], 0);
        }

        var sortedIds = await ApplySortAndPageAsync(
            schools,
            filter,
            searchTerm,
            academicYearId,
            paging,
            cancellationToken);

        if (sortedIds.Count == 0)
        {
            return ([], totalCount);
        }

        var projections = await BuildProjectionsAsync(
            sortedIds,
            filter,
            academicYearId,
            cancellationToken);

        return (projections, totalCount);
    }

    public async Task<School?> GetBySlugAsync(
        string slug,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = includeDetails
            ? BuildDetailedQuery()
            : dbContext.Schools.AsNoTracking();

        return await query.FirstOrDefaultAsync(school => school.Slug == slug, cancellationToken);
    }

    public async Task<PagedResult<School>> ListAsync(
        SchoolCatalogFilters filters,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Schools.AsNoTracking();
        query = ApplyCatalogFilter(query, filters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(school => school.NameAr)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToArrayAsync(cancellationToken);

        return PagedResult<School>.Create(items, totalCount, paging);
    }

    public Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeSchoolId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Schools.AsNoTracking().Where(school => school.Slug == slug);

        if (excludeSchoolId is { } schoolId)
        {
            query = query.Where(school => school.Id != schoolId);
        }

        return query.AnyAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveAcademicYearIdAsync(
        Guid? requestedYearId,
        CancellationToken cancellationToken)
    {
        if (requestedYearId is { } id)
        {
            return id;
        }

        return await dbContext.AcademicYears
            .AsNoTracking()
            .Where(year => year.IsActive && year.IsCurrent)
            .Select(year => (Guid?)year.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string? NormalizeSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        return MultiSpace.Replace(search.Trim(), " ");
    }

    private IQueryable<School> BuildDetailedQuery() =>
        dbContext.Schools
            .AsNoTracking()
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.City)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.District)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.StageOfferings)
                    .ThenInclude(offering => offering.EducationalStage)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.StageOfferings)
                    .ThenInclude(offering => offering.GradeOfferings)
                        .ThenInclude(gradeOffering => gradeOffering.Grade)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.TuitionFees)
                    .ThenInclude(fee => fee.EducationalStage)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.TuitionFees)
                    .ThenInclude(fee => fee.Grade)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.TuitionFees)
                    .ThenInclude(fee => fee.AcademicYear)
            .Include(school => school.Branches)
                .ThenInclude(branch => branch.TuitionFees)
                    .ThenInclude(fee => fee.Installments)
            .Include(school => school.Curricula)
                .ThenInclude(link => link.Curriculum)
            .Include(school => school.Facilities)
                .ThenInclude(link => link.Facility)
            .Include(school => school.Images)
            .Include(school => school.AdditionalServices)
            .Include(school => school.PublishedDiscounts)
            .Include(school => school.FinancialNotes);

    private static IQueryable<School> ApplyPublicFilter(
        IQueryable<School> query,
        PublicSchoolListFilter filter,
        string? searchTerm,
        Guid? academicYearId)
    {
        if (searchTerm is not null)
        {
            query = query.Where(school =>
                school.NameAr.Contains(searchTerm) ||
                (school.NameEn != null && school.NameEn.Contains(searchTerm)) ||
                (school.ShortDescriptionAr != null && school.ShortDescriptionAr.Contains(searchTerm)) ||
                (school.ShortDescriptionEn != null && school.ShortDescriptionEn.Contains(searchTerm)));
        }

        if (filter.CountryId is { } countryId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive
                    && branch.City.Governorate != null
                    && branch.City.Governorate.CountryId == countryId));
        }

        if (filter.GovernorateId is { } governorateId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive && branch.City.GovernorateId == governorateId));
        }

        if (filter.CityId is { } cityId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch => branch.IsActive && branch.CityId == cityId));
        }

        if (filter.DistrictId is { } districtId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch => branch.IsActive && branch.DistrictId == districtId));
        }

        if (filter.CurriculumIds is { Count: > 0 } curriculumIds)
        {
            query = query.Where(school =>
                school.Curricula.Any(link =>
                    curriculumIds.Contains(link.CurriculumId) && link.Curriculum.IsActive));
        }

        if (filter.StageId is { } stageId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive && offering.EducationalStageId == stageId)));
        }

        if (filter.GradeId is { } gradeId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive &&
                        (!filter.StageId.HasValue || offering.EducationalStageId == filter.StageId) &&
                        offering.GradeOfferings.Any(gradeOffering =>
                            gradeOffering.IsActive && gradeOffering.GradeId == gradeId))));
        }

        if (filter.SchoolType is { } schoolType)
        {
            query = query.Where(school => school.SchoolType == schoolType);
        }

        if (filter.GenderType is { } genderType)
        {
            query = query.Where(school => school.GenderType == genderType);
        }

        if (filter.AdmissionOpen is true)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive &&
                        offering.IsAdmissionOpen &&
                        (!filter.StageId.HasValue || offering.EducationalStageId == filter.StageId) &&
                        (!filter.GradeId.HasValue || offering.GradeOfferings.Any(gradeOffering =>
                            gradeOffering.IsActive && gradeOffering.GradeId == filter.GradeId)))));
        }
        else if (filter.AdmissionOpen is false)
        {
            query = query.Where(school =>
                !school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive && offering.IsAdmissionOpen)));
        }

        if (filter.FacilityIds is { Count: > 0 } facilityIds)
        {
            foreach (var facilityId in facilityIds.Distinct())
            {
                var requiredId = facilityId;
                query = query.Where(school =>
                    school.Facilities.Any(link =>
                        link.FacilityId == requiredId && link.Facility.IsActive));
            }
        }

        var hasTuitionFilter = filter.MinimumTuition.HasValue || filter.MaximumTuition.HasValue;
        if (hasTuitionFilter)
        {
            var utcNow = DateTimeOffset.UtcNow;
            // Schools without matching active published EGP fees for the resolved academic year are excluded.
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.TuitionFees.Any(fee =>
                        fee.IsActive &&
                        fee.IsPublished &&
                        fee.CurrencyCode == "EGP" &&
                        (fee.EffectiveFromUtc == null || fee.EffectiveFromUtc <= utcNow) &&
                        (fee.EffectiveToUtc == null || fee.EffectiveToUtc >= utcNow) &&
                        (!academicYearId.HasValue || fee.AcademicYearId == academicYearId) &&
                        (!filter.MinimumTuition.HasValue || fee.Amount >= filter.MinimumTuition) &&
                        (!filter.MaximumTuition.HasValue || fee.Amount <= filter.MaximumTuition))));
        }

        return query;
    }

    private async Task<List<Guid>> ApplySortAndPageAsync(
        IQueryable<School> schools,
        PublicSchoolListFilter filter,
        string? searchTerm,
        Guid? academicYearId,
        PagedRequest paging,
        CancellationToken cancellationToken)
    {
        var sort = filter.Sort;
        if (sort == PublicSchoolSort.Relevance && searchTerm is null)
        {
            sort = PublicSchoolSort.Newest;
        }

        if (sort == PublicSchoolSort.Nearest && filter.Latitude is { } nearLat && filter.Longitude is { } nearLon)
        {
            var candidates = await schools
                .Select(school => new
                {
                    school.Id,
                    school.CreatedAtUtc,
                    Latitude = school.Branches
                        .Where(branch => branch.IsActive && branch.Latitude != null)
                        .Select(branch => (double?)branch.Latitude)
                        .FirstOrDefault(),
                    Longitude = school.Branches
                        .Where(branch => branch.IsActive && branch.Longitude != null)
                        .Select(branch => (double?)branch.Longitude)
                        .FirstOrDefault(),
                })
                .ToListAsync(cancellationToken);

            return candidates
                .OrderBy(row => row.Latitude is null || row.Longitude is null)
                .ThenBy(row => ApproximateDistanceKm(
                    nearLat,
                    nearLon,
                    row.Latitude ?? 0,
                    row.Longitude ?? 0))
                .ThenByDescending(row => row.CreatedAtUtc)
                .ThenBy(row => row.Id)
                .Skip(paging.Skip)
                .Take(paging.NormalizedPageSize)
                .Select(row => row.Id)
                .ToList();
        }

        IQueryable<Guid> ordered = sort switch
        {
            PublicSchoolSort.NameAsc => schools
                .OrderBy(school => school.NameAr)
                .ThenBy(school => school.Id)
                .Select(school => school.Id),
            PublicSchoolSort.NameDesc => schools
                .OrderByDescending(school => school.NameAr)
                .ThenBy(school => school.Id)
                .Select(school => school.Id),
            PublicSchoolSort.Newest => schools
                .OrderByDescending(school => school.CreatedAtUtc)
                .ThenBy(school => school.Id)
                .Select(school => school.Id),
            PublicSchoolSort.LowestFee => OrderByFee(schools, academicYearId, ascending: true),
            PublicSchoolSort.HighestFee => OrderByFee(schools, academicYearId, ascending: false),
            PublicSchoolSort.Relevance when searchTerm is not null =>
                OrderByRelevance(schools, searchTerm),
            _ => schools
                .OrderByDescending(school => school.CreatedAtUtc)
                .ThenBy(school => school.Id)
                .Select(school => school.Id),
        };

        return await ordered
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Guid> OrderByFee(
        IQueryable<School> schools,
        Guid? academicYearId,
        bool ascending)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var withFee = schools.Select(school => new
        {
            school.Id,
            school.CreatedAtUtc,
            MinFee = school.Branches
                .Where(branch => branch.IsActive)
                .SelectMany(branch => branch.TuitionFees)
                .Where(fee =>
                    fee.IsActive &&
                    fee.IsPublished &&
                    fee.CurrencyCode == "EGP" &&
                    (fee.EffectiveFromUtc == null || fee.EffectiveFromUtc <= utcNow) &&
                    (fee.EffectiveToUtc == null || fee.EffectiveToUtc >= utcNow) &&
                    (!academicYearId.HasValue || fee.AcademicYearId == academicYearId))
                .Select(fee => (decimal?)fee.Amount)
                .Min(),
        });

        return ascending
            ? withFee
                .OrderBy(row => row.MinFee == null)
                .ThenBy(row => row.MinFee)
                .ThenByDescending(row => row.CreatedAtUtc)
                .ThenBy(row => row.Id)
                .Select(row => row.Id)
            : withFee
                .OrderBy(row => row.MinFee == null)
                .ThenByDescending(row => row.MinFee)
                .ThenByDescending(row => row.CreatedAtUtc)
                .ThenBy(row => row.Id)
                .Select(row => row.Id);
    }

    private static IQueryable<Guid> OrderByRelevance(IQueryable<School> schools, string searchTerm)
    {
        return schools.Select(school => new
        {
            school.Id,
            school.NameAr,
            Rank =
                school.NameAr == searchTerm || school.NameEn == searchTerm ? 0 :
                school.NameAr.StartsWith(searchTerm) || (school.NameEn != null && school.NameEn.StartsWith(searchTerm)) ? 1 :
                school.NameAr.Contains(searchTerm) || (school.NameEn != null && school.NameEn.Contains(searchTerm)) ? 2 :
                3,
        })
            .OrderBy(row => row.Rank)
            .ThenBy(row => row.NameAr)
            .ThenBy(row => row.Id)
            .Select(row => row.Id);
    }

    private async Task<IReadOnlyList<PublicSchoolSearchProjection>> BuildProjectionsAsync(
        IReadOnlyList<Guid> orderedIds,
        PublicSchoolListFilter filter,
        Guid? academicYearId,
        CancellationToken cancellationToken)
    {
        var idSet = orderedIds.ToArray();
        var schools = await dbContext.Schools
            .AsNoTracking()
            .Where(school => idSet.Contains(school.Id))
            .Select(school => new
            {
                school.Id,
                school.Slug,
                school.NameAr,
                school.NameEn,
                school.LogoUrl,
                school.CoverUrl,
                school.SchoolType,
                school.GenderType,
                school.CreatedAtUtc,
                school.FeeVisibilityPolicy,
            })
            .ToListAsync(cancellationToken);

        var branches = await dbContext.SchoolBranches
            .AsNoTracking()
            .Where(branch => branch.IsActive && idSet.Contains(branch.SchoolId))
            .Select(branch => new
            {
                branch.SchoolId,
                branch.IsMainBranch,
                branch.Latitude,
                branch.Longitude,
                CityNameAr = branch.City.NameAr,
                CityNameEn = branch.City.NameEn,
                DistrictNameAr = branch.District.NameAr,
                DistrictNameEn = branch.District.NameEn,
            })
            .ToListAsync(cancellationToken);

        var curricula = await dbContext.SchoolCurricula
            .AsNoTracking()
            .Where(link => idSet.Contains(link.SchoolId) && link.Curriculum.IsActive)
            .OrderBy(link => link.Curriculum.SortOrder)
            .Select(link => new
            {
                link.SchoolId,
                link.Curriculum.NameAr,
                link.Curriculum.NameEn,
            })
            .ToListAsync(cancellationToken);

        var stages = await dbContext.SchoolStageOfferings
            .AsNoTracking()
            .Where(offering =>
                offering.IsActive &&
                offering.SchoolBranch.IsActive &&
                idSet.Contains(offering.SchoolBranch.SchoolId))
            .Select(offering => new
            {
                offering.SchoolBranch.SchoolId,
                offering.IsAdmissionOpen,
                StageNameAr = offering.EducationalStage.NameAr,
                StageNameEn = offering.EducationalStage.NameEn,
                offering.EducationalStage.SortOrder,
            })
            .ToListAsync(cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        var fees = await dbContext.TuitionFees
            .AsNoTracking()
            .Where(fee =>
                fee.IsActive &&
                fee.IsPublished &&
                fee.CurrencyCode == "EGP" &&
                fee.SchoolBranch.IsActive &&
                idSet.Contains(fee.SchoolBranch.SchoolId) &&
                (fee.EffectiveFromUtc == null || fee.EffectiveFromUtc <= utcNow) &&
                (fee.EffectiveToUtc == null || fee.EffectiveToUtc >= utcNow) &&
                (!academicYearId.HasValue || fee.AcademicYearId == academicYearId))
            .Select(fee => new
            {
                fee.SchoolBranch.SchoolId,
                fee.Amount,
                fee.CurrencyCode,
            })
            .ToListAsync(cancellationToken);

        var isAuthenticatedParent = currentUser.IsAuthenticated &&
                                    currentUser.IsInRole(SchooleraRoles.Parent);

        IReadOnlySet<Guid> favoriteSchoolIds = new HashSet<Guid>();
        if (isAuthenticatedParent && currentUser.UserId is { } parentUserId)
        {
            favoriteSchoolIds = await favoriteSchoolRepository.GetFavoriteSchoolIdsAsync(
                parentUserId,
                orderedIds,
                cancellationToken);
        }

        var orderIndex = orderedIds
            .Select((id, index) => (id, index))
            .ToDictionary(pair => pair.id, pair => pair.index);

        var result = new List<PublicSchoolSearchProjection>(schools.Count);
        foreach (var school in schools.OrderBy(item => orderIndex[item.Id]))
        {
            var schoolBranches = branches.Where(branch => branch.SchoolId == school.Id).ToList();
            var main = schoolBranches.FirstOrDefault(branch => branch.IsMainBranch)
                ?? schoolBranches.FirstOrDefault();

            var schoolStages = stages
                .Where(stage => stage.SchoolId == school.Id)
                .OrderBy(stage => stage.SortOrder)
                .ToList();

            var schoolCurricula = curricula.Where(item => item.SchoolId == school.Id).ToList();
            var schoolFees = fees.Where(fee => fee.SchoolId == school.Id).ToList();
            var hasPublishedFees = schoolFees.Count > 0;
            var canReveal = FeeVisibilityResolver.CanRevealDetailedFees(
                school.FeeVisibilityPolicy,
                isAuthenticatedParent);
            var feesRequireLogin = FeeVisibilityResolver.FeesRequireLogin(school.FeeVisibilityPolicy) &&
                                   !canReveal;
            var minFee = !canReveal || schoolFees.Count == 0
                ? (decimal?)null
                : schoolFees.Min(fee => fee.Amount);

            double? distanceKm = null;
            if (filter.Latitude is { } lat && filter.Longitude is { } lon)
            {
                var best = schoolBranches
                    .Where(branch => branch.Latitude is not null && branch.Longitude is not null)
                    .Select(branch => ApproximateDistanceKm(
                        lat,
                        lon,
                        (double)branch.Latitude!.Value,
                        (double)branch.Longitude!.Value))
                    .DefaultIfEmpty()
                    .Min();
                if (best > 0 || schoolBranches.Any(branch => branch.Latitude is not null))
                {
                    distanceKm = best;
                }
            }

            bool? isFavorite = isAuthenticatedParent
                ? favoriteSchoolIds.Contains(school.Id)
                : null;

            result.Add(new PublicSchoolSearchProjection(
                school.Id,
                school.Slug,
                school.NameAr,
                school.NameEn,
                school.LogoUrl,
                school.CoverUrl,
                school.SchoolType,
                school.GenderType,
                school.CreatedAtUtc,
                main?.CityNameAr ?? string.Empty,
                main?.CityNameEn,
                main?.DistrictNameAr ?? string.Empty,
                main?.DistrictNameEn,
                schoolStages.Any(stage => stage.IsAdmissionOpen),
                schoolCurricula.Select(item => item.NameAr).Take(3).ToArray(),
                schoolCurricula.Select(item => item.NameEn ?? item.NameAr).Take(3).ToArray(),
                schoolStages.Select(stage => stage.StageNameAr).Distinct().Take(3).ToArray(),
                schoolStages.Select(stage => stage.StageNameEn ?? stage.StageNameAr).Distinct().Take(3).ToArray(),
                minFee,
                minFee is null ? null : "EGP",
                distanceKm,
                hasPublishedFees,
                feesRequireLogin,
                isFavorite));
        }

        return result;
    }

    private static double ApproximateDistanceKm(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        // Equirectangular approximation — consistent with nearest sort ranking units.
        const double degToRad = Math.PI / 180d;
        var x = (lon2 - lon1) * degToRad * Math.Cos((lat1 + lat2) * 0.5 * degToRad);
        var y = (lat2 - lat1) * degToRad;
        return Math.Sqrt(x * x + y * y) * 6371d;
    }

    private static IQueryable<School> ApplyCatalogFilter(
        IQueryable<School> query,
        SchoolCatalogFilters filters)
    {
        if (filters.Status is { } status)
        {
            query = query.Where(school => school.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(school =>
                school.NameAr.Contains(term) ||
                (school.NameEn != null && school.NameEn.Contains(term)));
        }

        if (filters.SchoolType is { } schoolType)
        {
            query = query.Where(school => school.SchoolType == schoolType);
        }

        if (filters.GenderType is { } genderType)
        {
            query = query.Where(school => school.GenderType == genderType);
        }

        if (filters.CityId is { } cityId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch => branch.IsActive && branch.CityId == cityId));
        }

        if (filters.DistrictId is { } districtId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch => branch.IsActive && branch.DistrictId == districtId));
        }

        if (filters.CurriculumId is { } curriculumId)
        {
            query = query.Where(school =>
                school.Curricula.Any(link => link.CurriculumId == curriculumId));
        }

        if (filters.EducationalStageId is { } stageId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive && offering.EducationalStageId == stageId)));
        }

        if (filters.GradeId is { } gradeId)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive &&
                        offering.GradeOfferings.Any(gradeOffering =>
                            gradeOffering.IsActive && gradeOffering.GradeId == gradeId))));
        }

        if (filters.AdmissionOpen is { } admissionOpen)
        {
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.StageOfferings.Any(offering =>
                        offering.IsActive && offering.IsAdmissionOpen == admissionOpen)));
        }

        if (filters.MinTuition is { } minTuition)
        {
            var utcNow = DateTimeOffset.UtcNow;
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.TuitionFees.Any(fee =>
                        fee.IsActive &&
                        fee.IsPublished &&
                        (fee.EffectiveFromUtc == null || fee.EffectiveFromUtc <= utcNow) &&
                        (fee.EffectiveToUtc == null || fee.EffectiveToUtc >= utcNow) &&
                        fee.Amount >= minTuition)));
        }

        if (filters.MaxTuition is { } maxTuition)
        {
            var utcNow = DateTimeOffset.UtcNow;
            query = query.Where(school =>
                school.Branches.Any(branch =>
                    branch.IsActive &&
                    branch.TuitionFees.Any(fee =>
                        fee.IsActive &&
                        fee.IsPublished &&
                        (fee.EffectiveFromUtc == null || fee.EffectiveFromUtc <= utcNow) &&
                        (fee.EffectiveToUtc == null || fee.EffectiveToUtc >= utcNow) &&
                        fee.Amount <= maxTuition)));
        }

        return query;
    }
}
