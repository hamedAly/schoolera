using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

public sealed class AdmissionApplicationRepository(SchooleraDbContext dbContext)
    : IAdmissionApplicationRepository
{
    private static readonly AdmissionApplicationStatus[] ActiveStatuses =
    [
        AdmissionApplicationStatus.Draft,
        AdmissionApplicationStatus.Submitted,
        AdmissionApplicationStatus.UnderReview,
        AdmissionApplicationStatus.Accepted,
        AdmissionApplicationStatus.MissingDocuments,
        AdmissionApplicationStatus.InterviewRequired,
        AdmissionApplicationStatus.AssessmentRequired,
        AdmissionApplicationStatus.WaitingList,
        AdmissionApplicationStatus.Registered,
    ];

    public Task<AdmissionApplication?> GetOwnedAsync(
        Guid parentUserId,
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        BuildDetailQuery(asNoTracking: true)
            .FirstOrDefaultAsync(
                application => application.Id == applicationId && application.ParentUserId == parentUserId,
                cancellationToken);

    public Task<AdmissionApplication?> GetOwnedForUpdateAsync(
        Guid parentUserId,
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        // Avoid History Includes (RowVersion churn). Include lifecycle graphs needed for MissingDocuments edits.
        dbContext.AdmissionApplications
            .Include(application => application.RequirementSnapshots)
            .Include(application => application.QuestionSnapshots)
            .Include(application => application.Answers)
            .Include(application => application.Attachments)
            .Include(application => application.PolicySnapshot)
            .Include(application => application.AgeEligibilitySnapshot)
            .Include(application => application.MissingItemsRequests)
                .ThenInclude(request => request.Items)
            .FirstOrDefaultAsync(
                application => application.Id == applicationId && application.ParentUserId == parentUserId,
                cancellationToken);

    public Task<AdmissionApplicationAttachment?> GetOwnedAttachmentAsync(
        Guid parentUserId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAttachments
            .Include(attachment => attachment.AdmissionApplication)
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.Id == attachmentId &&
                    attachment.AdmissionApplicationId == applicationId &&
                    attachment.AdmissionApplication.ParentUserId == parentUserId,
                cancellationToken);

    public Task<bool> HasActiveDuplicateAsync(
        Guid childProfileId,
        Guid schoolId,
        Guid schoolBranchId,
        Guid gradeId,
        Guid academicYearId,
        Guid? excludeApplicationId,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AdmissionApplications.AsNoTracking()
            .Where(application =>
                application.ChildProfileId == childProfileId &&
                application.SchoolId == schoolId &&
                application.SchoolBranchId == schoolBranchId &&
                application.GradeId == gradeId &&
                application.AcademicYearId == academicYearId &&
                ActiveStatuses.Contains(application.Status));

        if (excludeApplicationId is { } excludeId)
        {
            query = query.Where(application => application.Id != excludeId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasRequirementSnapshotsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationRequirementSnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.AdmissionApplicationId == applicationId, cancellationToken);

    public async Task<bool> HasInterviewOrAssessmentAppointmentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var hasInterview = await dbContext.AdmissionInterviewAppointments
            .AsNoTracking()
            .AnyAsync(item => item.AdmissionApplicationId == applicationId, cancellationToken);
        if (hasInterview)
        {
            return true;
        }

        return await dbContext.AdmissionAssessmentAppointments
            .AsNoTracking()
            .AnyAsync(item => item.AdmissionApplicationId == applicationId, cancellationToken);
    }

    public Task<bool> HasAnyForChildAsync(
        Guid childProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(application => application.ChildProfileId == childProfileId, cancellationToken);

    public async Task AddAsync(AdmissionApplication application, CancellationToken cancellationToken = default) =>
        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);

    public void AddHistory(AdmissionApplicationHistory entry) =>
        dbContext.AdmissionApplicationHistory.Add(entry);

    public void AddRequirementSnapshot(AdmissionApplicationRequirementSnapshot snapshot) =>
        dbContext.AdmissionApplicationRequirementSnapshots.Add(snapshot);

    public void AddAttachment(AdmissionApplicationAttachment attachment) =>
        dbContext.AdmissionApplicationAttachments.Add(attachment);

    public void AddAnswer(AdmissionApplicationAnswer answer) =>
        dbContext.AdmissionApplicationAnswers.Add(answer);

    public void RemoveAnswer(AdmissionApplicationAnswer answer) =>
        dbContext.AdmissionApplicationAnswers.Remove(answer);

    public Task<AdmissionApplicationAnswer?> GetOwnedAnswerForUpdateAsync(
        Guid parentUserId,
        Guid applicationId,
        Guid questionSnapshotId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAnswers
            .FirstOrDefaultAsync(
                answer =>
                    answer.AdmissionApplicationId == applicationId &&
                    answer.QuestionSnapshotId == questionSnapshotId &&
                    answer.AdmissionApplication.ParentUserId == parentUserId,
                cancellationToken);

    public void RemoveAttachment(AdmissionApplicationAttachment attachment) =>
        dbContext.AdmissionApplicationAttachments.Remove(attachment);

    public Task<AdmissionApplicationAttachment?> GetOwnedAttachmentForUpdateAsync(
        Guid parentUserId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAttachments
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.Id == attachmentId &&
                    attachment.AdmissionApplicationId == applicationId &&
                    attachment.AdmissionApplication.ParentUserId == parentUserId,
                cancellationToken);

    public async Task<PagedResult<AdmissionApplicationListItemDto>> ListForParentAsync(
        Guid parentUserId,
        AdmissionApplicationListQuery query,
        string preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(query.PageNumber, query.PageSize);
        var preferEnglish = preferredLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);

        var filtered = dbContext.AdmissionApplications.AsNoTracking()
            .Where(application => application.ParentUserId == parentUserId);

        if (query.Status is { } status)
        {
            filtered = filtered.Where(application => application.Status == status);
        }

        if (query.ChildProfileId is { } childProfileId)
        {
            filtered = filtered.Where(application => application.ChildProfileId == childProfileId);
        }

        if (query.SchoolId is { } schoolId)
        {
            filtered = filtered.Where(application => application.SchoolId == schoolId);
        }

        if (query.AcademicYearId is { } academicYearId)
        {
            filtered = filtered.Where(application => application.AcademicYearId == academicYearId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(application =>
                application.ApplicationNumber.Contains(term) ||
                application.School.NameAr.Contains(term) ||
                (application.School.NameEn != null && application.School.NameEn.Contains(term)));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var ordered = ApplySort(filtered, query.Sort);

        var rows = await ordered
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(application => new
            {
                application.Id,
                application.ApplicationNumber,
                application.Status,
                application.ChildProfileId,
                ChildLiveName = application.ChildProfile.FullName,
                application.SubmittedChildFullName,
                application.SchoolId,
                SchoolNameAr = application.School.NameAr,
                SchoolNameEn = application.School.NameEn,
                application.SubmittedSchoolNameAr,
                application.SubmittedSchoolNameEn,
                application.SchoolBranchId,
                BranchNameAr = application.SchoolBranch.NameAr,
                BranchNameEn = application.SchoolBranch.NameEn,
                application.SubmittedBranchNameAr,
                application.SubmittedBranchNameEn,
                application.EducationalStageId,
                StageNameAr = application.EducationalStage.NameAr,
                StageNameEn = application.EducationalStage.NameEn,
                application.SubmittedStageNameAr,
                application.SubmittedStageNameEn,
                application.GradeId,
                GradeNameAr = application.Grade.NameAr,
                GradeNameEn = application.Grade.NameEn,
                application.SubmittedGradeNameAr,
                application.SubmittedGradeNameEn,
                application.AcademicYearId,
                YearNameAr = application.AcademicYear.NameAr,
                YearNameEn = application.AcademicYear.NameEn,
                application.SubmittedAcademicYearNameAr,
                application.SubmittedAcademicYearNameEn,
                application.CreatedAtUtc,
                application.SubmittedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(row =>
            {
                var useSnapshot = row.Status != AdmissionApplicationStatus.Draft;
                return new AdmissionApplicationListItemDto(
                    row.Id,
                    row.ApplicationNumber,
                    row.Status,
                    row.ChildProfileId,
                    useSnapshot
                        ? FirstNonEmpty(row.SubmittedChildFullName, row.ChildLiveName)
                        : FirstNonEmpty(row.ChildLiveName, row.SubmittedChildFullName),
                    row.SchoolId,
                    PickLocalized(
                        preferEnglish,
                        useSnapshot,
                        row.SchoolNameAr,
                        row.SchoolNameEn,
                        row.SubmittedSchoolNameAr,
                        row.SubmittedSchoolNameEn),
                    row.SchoolBranchId,
                    PickLocalized(
                        preferEnglish,
                        useSnapshot,
                        row.BranchNameAr,
                        row.BranchNameEn,
                        row.SubmittedBranchNameAr,
                        row.SubmittedBranchNameEn),
                    row.EducationalStageId,
                    PickLocalized(
                        preferEnglish,
                        useSnapshot,
                        row.StageNameAr,
                        row.StageNameEn,
                        row.SubmittedStageNameAr,
                        row.SubmittedStageNameEn),
                    row.GradeId,
                    PickLocalized(
                        preferEnglish,
                        useSnapshot,
                        row.GradeNameAr,
                        row.GradeNameEn,
                        row.SubmittedGradeNameAr,
                        row.SubmittedGradeNameEn),
                    row.AcademicYearId,
                    PickLocalized(
                        preferEnglish,
                        useSnapshot,
                        row.YearNameAr,
                        row.YearNameEn,
                        row.SubmittedAcademicYearNameAr,
                        row.SubmittedAcademicYearNameEn),
                    row.CreatedAtUtc,
                    row.SubmittedAtUtc);
            })
            .ToArray();

        return PagedResult<AdmissionApplicationListItemDto>.Create(items, totalCount, paging);
    }

    public async Task<Dictionary<AdmissionApplicationStatus, int>> CountByStatusForParentAsync(
        Guid parentUserId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.AdmissionApplications.AsNoTracking()
            .Where(application => application.ParentUserId == parentUserId)
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Status, row => row.Count);
    }

    public async Task<IReadOnlyList<ParentRecentActivityDto>> ListRecentParentVisibleActivityAsync(
        Guid parentUserId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var limit = take < 1 ? 1 : take;

        return await dbContext.AdmissionApplicationHistory.AsNoTracking()
            .Where(history =>
                history.ParentVisible &&
                history.AdmissionApplication.ParentUserId == parentUserId)
            .OrderByDescending(history => history.CreatedAtUtc)
            .Take(limit)
            .Select(history => new ParentRecentActivityDto(
                history.Action,
                history.CreatedAtUtc,
                history.ParentVisibleNote ?? history.Action))
            .ToListAsync(cancellationToken);
    }

    public Task<AdmissionApplication?> GetForSchoolAsync(
        Guid schoolId,
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        BuildDetailQuery(asNoTracking: true)
            .Include(application => application.ParentProfile)
            .Include(application => application.ChildProfile)
                .ThenInclude(child => child.CurrentGrade)
            .FirstOrDefaultAsync(
                application => application.Id == applicationId && application.SchoolId == schoolId,
                cancellationToken);

    public Task<AdmissionApplication?> GetForSchoolForUpdateAsync(
        Guid schoolId,
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplications
            .Include(application => application.RequirementSnapshots)
            .Include(application => application.QuestionSnapshots)
            .Include(application => application.Attachments)
            .Include(application => application.PolicySnapshot)
            .Include(application => application.AgeEligibilitySnapshot)
            .Include(application => application.MissingItemsRequests)
                .ThenInclude(request => request.Items)
            .Include(application => application.InterviewAppointments)
            .Include(application => application.AssessmentAppointments)
            .FirstOrDefaultAsync(
                application => application.Id == applicationId && application.SchoolId == schoolId,
                cancellationToken);

    public Task<AdmissionApplicationAttachment?> GetSchoolAttachmentAsync(
        Guid schoolId,
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAttachments
            .Include(attachment => attachment.AdmissionApplication)
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.Id == attachmentId &&
                    attachment.AdmissionApplicationId == applicationId &&
                    attachment.AdmissionApplication.SchoolId == schoolId,
                cancellationToken);

    public async Task<PagedResult<SchoolAdmissionApplicationListItemDto>> ListForSchoolFilteredAsync(
        Guid schoolId,
        SchoolAdmissionApplicationListQuery query,
        string preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(query.PageNumber, query.PageSize);
        var preferEnglish = preferredLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);
        var filtered = ApplySchoolFilters(
            dbContext.AdmissionApplications.AsNoTracking().Where(a => a.SchoolId == schoolId),
            query);

        var totalCount = await filtered.CountAsync(cancellationToken);
        var ordered = ApplySchoolSort(filtered, query.Sort);

        var rows = await ordered
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(application => new
            {
                application.Id,
                application.ApplicationNumber,
                application.Status,
                StudentName = application.SubmittedChildFullName ?? application.ChildProfile.FullName,
                ParentUserId = application.ParentUserId,
                SchoolNameAr = application.School.NameAr,
                SchoolNameEn = application.School.NameEn,
                BranchNameAr = application.SchoolBranch.NameAr,
                BranchNameEn = application.SchoolBranch.NameEn,
                StageNameAr = application.EducationalStage.NameAr,
                StageNameEn = application.EducationalStage.NameEn,
                GradeNameAr = application.Grade.NameAr,
                GradeNameEn = application.Grade.NameEn,
                YearNameAr = application.AcademicYear.NameAr,
                YearNameEn = application.AcademicYear.NameEn,
                application.CreatedAtUtc,
                application.SubmittedAtUtc,
                application.ReviewStartedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var parentIds = rows.Select(row => row.ParentUserId).Distinct().ToArray();
        var parentNames = await LoadParentDisplayNamesAsync(parentIds, cancellationToken);

        var items = rows.Select(row => new SchoolAdmissionApplicationListItemDto(
            row.Id,
            row.ApplicationNumber,
            row.Status,
            row.StudentName ?? string.Empty,
            parentNames.GetValueOrDefault(row.ParentUserId, string.Empty),
            PreferLanguage(preferEnglish, row.SchoolNameAr, row.SchoolNameEn),
            PreferLanguage(preferEnglish, row.BranchNameAr, row.BranchNameEn),
            PreferLanguage(preferEnglish, row.StageNameAr, row.StageNameEn),
            PreferLanguage(preferEnglish, row.GradeNameAr, row.GradeNameEn),
            PreferLanguage(preferEnglish, row.YearNameAr, row.YearNameEn),
            row.CreatedAtUtc,
            row.SubmittedAtUtc,
            row.ReviewStartedAtUtc,
            SchoolAdmissionCapabilityFactory.From(row.Status))).ToArray();

        return PagedResult<SchoolAdmissionApplicationListItemDto>.Create(items, totalCount, paging);
    }

    public async Task<Dictionary<AdmissionApplicationStatus, int>> CountByStatusForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.AdmissionApplications.AsNoTracking()
            .Where(application => application.SchoolId == schoolId)
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Status, row => row.Count);
    }

    public async Task<IReadOnlyList<SchoolAdmissionRecentItemDto>> ListRecentSubmittedForSchoolAsync(
        Guid schoolId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var limit = take < 1 ? 1 : Math.Min(take, 20);
        return await dbContext.AdmissionApplications.AsNoTracking()
            .Where(application =>
                application.SchoolId == schoolId &&
                application.Status != AdmissionApplicationStatus.Draft &&
                application.SubmittedAtUtc != null)
            .OrderByDescending(application => application.SubmittedAtUtc)
            .Take(limit)
            .Select(application => new SchoolAdmissionRecentItemDto(
                application.Id,
                application.ApplicationNumber,
                application.SubmittedChildFullName ?? application.ChildProfile.FullName,
                application.Status,
                application.SubmittedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public Task<AdmissionApplication?> GetForAdminAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default) =>
        BuildDetailQuery(asNoTracking: true)
            .Include(application => application.ParentProfile)
            .Include(application => application.SchoolBranch).ThenInclude(branch => branch.City)
            .FirstOrDefaultAsync(application => application.Id == applicationId, cancellationToken);

    public Task<AdmissionApplicationAttachment?> GetAdminAttachmentAsync(
        Guid applicationId,
        Guid attachmentId,
        CancellationToken cancellationToken = default) =>
        dbContext.AdmissionApplicationAttachments
            .Include(attachment => attachment.AdmissionApplication)
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.Id == attachmentId &&
                    attachment.AdmissionApplicationId == applicationId,
                cancellationToken);

    public async Task<PagedResult<AdminAdmissionApplicationListItemDto>> ListForAdminAsync(
        AdminAdmissionApplicationListQuery query,
        string preferredLanguage,
        CancellationToken cancellationToken = default)
    {
        var paging = new PagedRequest(query.PageNumber, query.PageSize);
        var preferEnglish = preferredLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);
        var filtered = ApplyAdminFilters(dbContext.AdmissionApplications.AsNoTracking(), query);
        var totalCount = await filtered.CountAsync(cancellationToken);
        var ordered = ApplySchoolSort(filtered, query.Sort);

        var rows = await ordered
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .Select(application => new
            {
                application.Id,
                application.ApplicationNumber,
                application.Status,
                application.SchoolId,
                SchoolNameAr = application.School.NameAr,
                SchoolNameEn = application.School.NameEn,
                CityNameAr = application.SchoolBranch.City.NameAr,
                CityNameEn = application.SchoolBranch.City.NameEn,
                BranchNameAr = application.SchoolBranch.NameAr,
                BranchNameEn = application.SchoolBranch.NameEn,
                StudentName = application.SubmittedChildFullName ?? application.ChildProfile.FullName,
                application.ParentUserId,
                GradeNameAr = application.Grade.NameAr,
                GradeNameEn = application.Grade.NameEn,
                YearNameAr = application.AcademicYear.NameAr,
                YearNameEn = application.AcademicYear.NameEn,
                application.SubmittedAtUtc,
                application.ReviewStartedAtUtc,
                application.AcceptedAtUtc,
                application.RejectedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var parentNames = await LoadParentDisplayNamesAsync(
            rows.Select(row => row.ParentUserId).Distinct().ToArray(),
            cancellationToken);

        var items = rows.Select(row => new AdminAdmissionApplicationListItemDto(
            row.Id,
            row.ApplicationNumber,
            row.Status,
            row.SchoolId,
            PreferLanguage(preferEnglish, row.SchoolNameAr, row.SchoolNameEn),
            PreferLanguage(preferEnglish, row.CityNameAr, row.CityNameEn),
            PreferLanguage(preferEnglish, row.BranchNameAr, row.BranchNameEn),
            row.StudentName ?? string.Empty,
            parentNames.GetValueOrDefault(row.ParentUserId, string.Empty),
            PreferLanguage(preferEnglish, row.GradeNameAr, row.GradeNameEn),
            PreferLanguage(preferEnglish, row.YearNameAr, row.YearNameEn),
            row.SubmittedAtUtc,
            row.ReviewStartedAtUtc,
            row.AcceptedAtUtc ?? row.RejectedAtUtc)).ToArray();

        return PagedResult<AdminAdmissionApplicationListItemDto>.Create(items, totalCount, paging);
    }

    public async Task<IReadOnlyList<AdminAdmissionExportRowDto>> ListForAdminExportAsync(
        AdminAdmissionApplicationListQuery query,
        string preferredLanguage,
        int maxRows,
        bool includeAnswers = false,
        IReadOnlyList<string>? answerColumns = null,
        CancellationToken cancellationToken = default)
    {
        var preferEnglish = preferredLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);
        var limit = maxRows < 1 ? 1 : Math.Min(maxRows, 10_000);
        var filtered = ApplyAdminFilters(dbContext.AdmissionApplications.AsNoTracking(), query);
        var ordered = ApplySchoolSort(filtered, query.Sort);

        var rows = await ordered
            .Take(limit)
            .Select(application => new
            {
                application.Id,
                application.ApplicationNumber,
                application.Status,
                SchoolNameAr = application.School.NameAr,
                SchoolNameEn = application.School.NameEn,
                CityNameAr = application.SchoolBranch.City.NameAr,
                CityNameEn = application.SchoolBranch.City.NameEn,
                BranchNameAr = application.SchoolBranch.NameAr,
                BranchNameEn = application.SchoolBranch.NameEn,
                StudentName = application.SubmittedChildFullName ?? application.ChildProfile.FullName,
                application.ParentUserId,
                GradeNameAr = application.Grade.NameAr,
                GradeNameEn = application.Grade.NameEn,
                YearNameAr = application.AcademicYear.NameAr,
                YearNameEn = application.AcademicYear.NameEn,
                application.SubmittedAtUtc,
                application.ReviewStartedAtUtc,
                application.AcceptedAtUtc,
                application.RejectedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var parentNames = await LoadParentDisplayNamesAsync(
            rows.Select(row => row.ParentUserId).Distinct().ToArray(),
            cancellationToken);

        Dictionary<Guid, IReadOnlyList<AdmissionApplicationAnswer>> answersByApplication = new();
        if (includeAnswers && rows.Count > 0)
        {
            var applicationIds = rows.Select(row => row.Id).ToArray();
            var answers = await dbContext.AdmissionApplicationAnswers
                .AsNoTracking()
                .Include(answer => answer.QuestionSnapshot)
                .Where(answer => applicationIds.Contains(answer.AdmissionApplicationId))
                .ToListAsync(cancellationToken);
            answersByApplication = answers
                .GroupBy(answer => answer.AdmissionApplicationId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<AdmissionApplicationAnswer>)group.ToList());
        }

        return rows.Select(row =>
        {
            var answerValues = includeAnswers
                ? BuildAnswerValues(
                    answersByApplication.GetValueOrDefault(row.Id) ?? [],
                    answerColumns ?? [])
                : Array.Empty<string>();

            return new AdminAdmissionExportRowDto(
                row.ApplicationNumber,
                row.Status.ToString(),
                PreferLanguage(preferEnglish, row.SchoolNameAr, row.SchoolNameEn),
                PreferLanguage(preferEnglish, row.CityNameAr, row.CityNameEn),
                PreferLanguage(preferEnglish, row.BranchNameAr, row.BranchNameEn),
                row.StudentName ?? string.Empty,
                parentNames.GetValueOrDefault(row.ParentUserId, string.Empty),
                PreferLanguage(preferEnglish, row.GradeNameAr, row.GradeNameEn),
                PreferLanguage(preferEnglish, row.YearNameAr, row.YearNameEn),
                row.SubmittedAtUtc?.ToString("O") ?? string.Empty,
                row.ReviewStartedAtUtc?.ToString("O") ?? string.Empty,
                (row.AcceptedAtUtc ?? row.RejectedAtUtc)?.ToString("O") ?? string.Empty,
                answerValues);
        }).ToArray();
    }

    public async Task<IReadOnlyList<string>> ResolveExportAnswerColumnsAsync(
        AdminAdmissionApplicationListQuery query,
        int maxColumns,
        CancellationToken cancellationToken = default)
    {
        var limit = maxColumns < 1 ? 1 : Math.Min(maxColumns, AdmissionQuestionCatalog.MaxExportAnswerColumns);
        var filtered = ApplyAdminFilters(dbContext.AdmissionApplications.AsNoTracking(), query);
        var applicationIds = await filtered.Select(application => application.Id).Take(5000).ToListAsync(cancellationToken);
        if (applicationIds.Count == 0)
        {
            return [];
        }

        return await dbContext.AdmissionApplicationQuestionSnapshots
            .AsNoTracking()
            .Where(snapshot => applicationIds.Contains(snapshot.AdmissionApplicationId))
            .GroupBy(snapshot => snapshot.QuestionCode)
            .Select(group => new
            {
                group.Key,
                SortOrder = group.Min(snapshot => snapshot.SortOrder),
            })
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Key)
            .Take(limit)
            .Select(item => item.Key)
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyList<string> BuildAnswerValues(
        IReadOnlyList<AdmissionApplicationAnswer> answers,
        IReadOnlyList<string> answerColumns)
    {
        if (answerColumns.Count == 0)
        {
            return [];
        }

        var byCode = answers.ToDictionary(
            answer => answer.QuestionSnapshot.QuestionCode,
            answer => FormatAnswerValue(answer),
            StringComparer.OrdinalIgnoreCase);

        return answerColumns
            .Select(code => byCode.GetValueOrDefault(code) ?? string.Empty)
            .ToArray();
    }

    private static string FormatAnswerValue(AdmissionApplicationAnswer answer)
    {
        if (!string.IsNullOrWhiteSpace(answer.TextValue))
        {
            return answer.TextValue;
        }

        if (answer.SelectedOptionCodes is not null)
        {
            return answer.SelectedOptionCodes;
        }

        if (answer.DateValue is { } date)
        {
            return date.ToString("O");
        }

        if (answer.BooleanValue is { } boolean)
        {
            return boolean ? "yes" : "no";
        }

        if (answer.AttachmentId is { } attachmentId)
        {
            return attachmentId.ToString("N");
        }

        return string.Empty;
    }

    public async Task<AdminAdmissionDashboardMetricsDto> GetAdminDashboardMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var rows = await dbContext.AdmissionApplications.AsNoTracking()
            .GroupBy(application => application.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        int Count(AdmissionApplicationStatus status) =>
            rows.FirstOrDefault(row => row.Status == status)?.Count ?? 0;

        var total = rows.Sum(row => row.Count);
        var applicationsToday = await dbContext.AdmissionApplications.AsNoTracking()
            .CountAsync(
                application => application.CreatedAtUtc >= today && application.CreatedAtUtc < tomorrow,
                cancellationToken);

        return new AdminAdmissionDashboardMetricsDto(
            TotalApplications: total,
            Draft: Count(AdmissionApplicationStatus.Draft),
            Submitted: Count(AdmissionApplicationStatus.Submitted),
            UnderReview: Count(AdmissionApplicationStatus.UnderReview),
            MissingDocuments: Count(AdmissionApplicationStatus.MissingDocuments),
            InterviewRequired: Count(AdmissionApplicationStatus.InterviewRequired),
            AssessmentRequired: Count(AdmissionApplicationStatus.AssessmentRequired),
            WaitingList: Count(AdmissionApplicationStatus.WaitingList),
            Accepted: Count(AdmissionApplicationStatus.Accepted),
            Rejected: Count(AdmissionApplicationStatus.Rejected),
            Cancelled: Count(AdmissionApplicationStatus.Cancelled),
            Registered: Count(AdmissionApplicationStatus.Registered),
            ApplicationsToday: applicationsToday,
            PendingSchoolReview: Count(AdmissionApplicationStatus.Submitted) +
                Count(AdmissionApplicationStatus.UnderReview) +
                Count(AdmissionApplicationStatus.MissingDocuments) +
                Count(AdmissionApplicationStatus.InterviewRequired) +
                Count(AdmissionApplicationStatus.AssessmentRequired) +
                Count(AdmissionApplicationStatus.WaitingList));
    }

    public async Task<PagedResult<AdmissionApplication>> ListForSchoolAsync(
        Guid schoolId,
        AdmissionApplicationStatus? status,
        PagedRequest paging,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.AdmissionApplications.AsNoTracking()
            .Include(application => application.ChildProfile)
            .Include(application => application.SchoolBranch)
            .Include(application => application.EducationalStage)
            .Include(application => application.Grade)
            .Include(application => application.AcademicYear)
            .Where(application => application.SchoolId == schoolId);

        if (status is { } filterStatus)
        {
            query = query.Where(application => application.Status == filterStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
            .ThenByDescending(application => application.CreatedAtUtc)
            .Skip(paging.Skip)
            .Take(paging.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<AdmissionApplication>.Create(items, totalCount, paging);
    }

    private async Task<Dictionary<Guid, string>> LoadParentDisplayNamesAsync(
        IReadOnlyCollection<Guid> parentUserIds,
        CancellationToken cancellationToken)
    {
        if (parentUserIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Users.AsNoTracking()
            .Where(user => parentUserIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                Name = ((user.FirstName ?? string.Empty) + " " + (user.LastName ?? string.Empty)).Trim(),
            })
            .ToDictionaryAsync(
                row => row.Id,
                row => string.IsNullOrWhiteSpace(row.Name) ? (row.Id.ToString()) : row.Name,
                cancellationToken);
    }

    private static IQueryable<AdmissionApplication> ApplySchoolFilters(
        IQueryable<AdmissionApplication> query,
        SchoolAdmissionApplicationListQuery filters)
    {
        if (filters.Status is { } status)
        {
            query = query.Where(application => application.Status == status);
        }

        if (filters.BranchId is { } branchId)
        {
            query = query.Where(application => application.SchoolBranchId == branchId);
        }
        else if (filters.RestrictToBranchIds is { Count: > 0 } restricted)
        {
            query = query.Where(application => restricted.Contains(application.SchoolBranchId));
        }

        if (filters.GradeId is { } gradeId)
        {
            query = query.Where(application => application.GradeId == gradeId);
        }

        if (filters.EducationalStageId is { } stageId)
        {
            query = query.Where(application => application.EducationalStageId == stageId);
        }

        if (filters.AcademicYearId is { } yearId)
        {
            query = query.Where(application => application.AcademicYearId == yearId);
        }

        if (filters.DateFrom is { } from)
        {
            query = query.Where(application =>
                (application.SubmittedAtUtc ?? application.CreatedAtUtc) >= from);
        }

        if (filters.DateTo is { } to)
        {
            query = query.Where(application =>
                (application.SubmittedAtUtc ?? application.CreatedAtUtc) <= to);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(application =>
                application.ApplicationNumber.Contains(term) ||
                application.ChildProfile.FullName.Contains(term) ||
                (application.SubmittedChildFullName != null &&
                 application.SubmittedChildFullName.Contains(term)));
        }

        return query;
    }

    private static IQueryable<AdmissionApplication> ApplyAdminFilters(
        IQueryable<AdmissionApplication> query,
        AdminAdmissionApplicationListQuery filters)
    {
        if (filters.SchoolId is { } schoolId)
        {
            query = query.Where(application => application.SchoolId == schoolId);
        }

        if (filters.CityId is { } cityId)
        {
            query = query.Where(application => application.SchoolBranch.CityId == cityId);
        }

        if (filters.Status is { } status)
        {
            query = query.Where(application => application.Status == status);
        }

        if (filters.BranchId is { } branchId)
        {
            query = query.Where(application => application.SchoolBranchId == branchId);
        }

        if (filters.GradeId is { } gradeId)
        {
            query = query.Where(application => application.GradeId == gradeId);
        }

        if (filters.AcademicYearId is { } yearId)
        {
            query = query.Where(application => application.AcademicYearId == yearId);
        }

        if (filters.DateFrom is { } from)
        {
            query = query.Where(application =>
                (application.SubmittedAtUtc ?? application.CreatedAtUtc) >= from);
        }

        if (filters.DateTo is { } to)
        {
            query = query.Where(application =>
                (application.SubmittedAtUtc ?? application.CreatedAtUtc) <= to);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(application =>
                application.ApplicationNumber.Contains(term) ||
                application.ChildProfile.FullName.Contains(term) ||
                (application.SubmittedChildFullName != null &&
                 application.SubmittedChildFullName.Contains(term)) ||
                application.School.NameAr.Contains(term) ||
                (application.School.NameEn != null && application.School.NameEn.Contains(term)));
        }

        return query;
    }

    private static IOrderedQueryable<AdmissionApplication> ApplySchoolSort(
        IQueryable<AdmissionApplication> query,
        string sort) =>
        sort.Trim().ToLowerInvariant() switch
        {
            "oldest" => query
                .OrderBy(application => application.CreatedAtUtc)
                .ThenBy(application => application.Id),
            "submitted-newest" => query
                .OrderByDescending(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
                .ThenByDescending(application => application.Id),
            "submitted-oldest" => query
                .OrderBy(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
                .ThenBy(application => application.Id),
            "application-number" => query
                .OrderBy(application => application.ApplicationNumber)
                .ThenBy(application => application.Id),
            "status" => query
                .OrderBy(application => application.Status)
                .ThenByDescending(application => application.SubmittedAtUtc ?? application.CreatedAtUtc)
                .ThenBy(application => application.Id),
            _ => query
                .OrderByDescending(application => application.CreatedAtUtc)
                .ThenByDescending(application => application.Id),
        };

    private IQueryable<AdmissionApplication> BuildDetailQuery(bool asNoTracking)
    {
        IQueryable<AdmissionApplication> query = dbContext.AdmissionApplications
            .Include(application => application.ChildProfile)
            .Include(application => application.School)
            .Include(application => application.SchoolBranch)
            .Include(application => application.EducationalStage)
            .Include(application => application.Grade)
            .Include(application => application.AcademicYear)
            .Include(application => application.Attachments)
            .Include(application => application.RequirementSnapshots)
            .Include(application => application.QuestionSnapshots)
                .ThenInclude(snapshot => snapshot.Options)
            .Include(application => application.Answers)
            .Include(application => application.History)
            .Include(application => application.PolicySnapshot)
            .Include(application => application.AgeEligibilitySnapshot)
            .Include(application => application.MissingItemsRequests)
                .ThenInclude(request => request.Items)
            .Include(application => application.InterviewAppointments)
            .Include(application => application.AssessmentAppointments);

        return asNoTracking ? query.AsNoTracking() : query;
    }

    private static IOrderedQueryable<AdmissionApplication> ApplySort(
        IQueryable<AdmissionApplication> query,
        string sort) =>
        sort.Trim().ToLowerInvariant() switch
        {
            "oldest" => query
                .OrderBy(application => application.CreatedAtUtc)
                .ThenBy(application => application.Id),
            "application-number" => query
                .OrderBy(application => application.ApplicationNumber)
                .ThenBy(application => application.Id),
            "status" => query
                .OrderBy(application => application.Status)
                .ThenByDescending(application => application.CreatedAtUtc)
                .ThenBy(application => application.Id),
            _ => query
                .OrderByDescending(application => application.CreatedAtUtc)
                .ThenByDescending(application => application.Id),
        };

    private static string PickLocalized(
        bool preferEnglish,
        bool preferSnapshot,
        string? liveAr,
        string? liveEn,
        string? snapshotAr,
        string? snapshotEn)
    {
        if (preferSnapshot)
        {
            var snapshot = PreferLanguage(preferEnglish, snapshotAr, snapshotEn);
            if (!string.IsNullOrWhiteSpace(snapshot))
            {
                return snapshot;
            }

            return PreferLanguage(preferEnglish, liveAr, liveEn);
        }

        var live = PreferLanguage(preferEnglish, liveAr, liveEn);
        if (!string.IsNullOrWhiteSpace(live))
        {
            return live;
        }

        return PreferLanguage(preferEnglish, snapshotAr, snapshotEn);
    }

    /// <summary>
    /// preferredLanguage "en" â†’ NameEn ?? NameAr; otherwise NameAr.
    /// </summary>
    private static string PreferLanguage(bool preferEnglish, string? nameAr, string? nameEn) =>
        preferEnglish
            ? nameEn ?? nameAr ?? string.Empty
            : nameAr ?? string.Empty;

    private static string FirstNonEmpty(string? primary, string? fallback) =>
        !string.IsNullOrWhiteSpace(primary)
            ? primary
            : fallback ?? string.Empty;
}
