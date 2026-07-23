using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence;

/// <summary>
/// Idempotent demo admission applications for the seeded parent account.
/// Runs after auth users and catalog/portal data exist.
/// Additive: existing DBs that already skipped the original seed still get a second child
/// and Accepted/Rejected fixtures when missing.
/// Metadata-only: never writes private files or attachment rows (no fake PDFs).
/// </summary>
public sealed class AdmissionApplicationSeeder(
    SchooleraDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IAdmissionApplicationNumberGenerator numberGenerator,
    IChildIdentityProtector identityProtector,
    ILogger<AdmissionApplicationSeeder> logger)
{
    private const string ParentEmail = "parent@schoolera.local";
    private const string DemoSchoolSlug = "cairo-international-school";
    private const string SeedChildIdentity = "29801011234567";
    private const string SeedChildIdentity2 = "29902021234568";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running admission application seed...");

        var parent = await userManager.FindByEmailAsync(ParentEmail);
        if (parent is null)
        {
            logger.LogInformation("Admission seed skipped; parent {Email} not found.", ParentEmail);
            return;
        }

        var parentProfile = await dbContext.ParentProfiles
            .FirstOrDefaultAsync(profile => profile.UserId == parent.Id, cancellationToken);

        if (parentProfile is null)
        {
            parentProfile = new ParentProfile(parent.Id);
            dbContext.ParentProfiles.Add(parentProfile);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var school = await dbContext.Schools
            .FirstOrDefaultAsync(
                item => item.Slug == DemoSchoolSlug && item.Status == SchoolStatus.Published,
                cancellationToken);

        if (school is null)
        {
            logger.LogInformation("Admission seed skipped; published school {Slug} not found.", DemoSchoolSlug);
            return;
        }

        var branch = await dbContext.SchoolBranches
            .FirstOrDefaultAsync(
                item => item.SchoolId == school.Id && item.IsActive,
                cancellationToken);

        if (branch is null)
        {
            logger.LogInformation("Admission seed skipped; no active branch for {Slug}.", DemoSchoolSlug);
            return;
        }

        var academicYear = await dbContext.AcademicYears
            .Where(item => item.IsActive)
            .OrderByDescending(item => item.IsCurrent)
            .ThenByDescending(item => item.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (academicYear is null)
        {
            logger.LogInformation("Admission seed skipped; no active academic year.");
            return;
        }

        var offerings = await dbContext.SchoolStageOfferings
            .AsNoTracking()
            .Include(item => item.GradeOfferings)
            .ThenInclude(gradeOffering => gradeOffering.Grade)
            .Include(item => item.EducationalStage)
            .Where(item =>
                item.SchoolBranchId == branch.Id &&
                item.IsActive &&
                item.IsAdmissionOpen)
            .OrderBy(item => item.EducationalStage.SortOrder)
            .ToListAsync(cancellationToken);

        var distinctSlots = offerings
            .SelectMany(offering => offering.GradeOfferings
                .Where(gradeOffering => gradeOffering.IsActive)
                .OrderBy(gradeOffering => gradeOffering.Grade.SortOrder)
                .Select(gradeOffering => new SeedSlot(
                    offering.EducationalStageId,
                    gradeOffering.GradeId)))
            .DistinctBy(slot => (slot.EducationalStageId, slot.GradeId))
            .ToList();

        if (distinctSlots.Count == 0)
        {
            logger.LogInformation("Admission seed skipped; no open grade offerings for {Slug}.", DemoSchoolSlug);
            return;
        }

        var child1 = await EnsureChildAsync(
            parent.Id,
            parentProfile.Id,
            SeedChildIdentity,
            fullName: "نور أحمد حسن",
            birthDate: new DateOnly(2015, 3, 12),
            ChildGender.Male,
            distinctSlots[0].GradeId,
            cancellationToken);

        var child2 = await EnsureChildAsync(
            parent.Id,
            parentProfile.Id,
            SeedChildIdentity2,
            fullName: "سارة أحمد حسن",
            birthDate: new DateOnly(2017, 8, 21),
            ChildGender.Female,
            distinctSlots.Count > 1 ? distinctSlots[1].GradeId : distinctSlots[0].GradeId,
            cancellationToken);

        var hasAnyApplication = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(application => application.ParentUserId == parent.Id, cancellationToken);

        if (!hasAnyApplication)
        {
            var draftSlot = distinctSlots[0];
            await SeedDraftAsync(parent.Id, parentProfile.Id, child1, school, branch, draftSlot, academicYear, cancellationToken);

            if (distinctSlots.Count >= 2)
            {
                await SeedSubmittedAsync(
                    parent.Id,
                    parentProfile.Id,
                    child1,
                    school,
                    branch,
                    distinctSlots[1],
                    academicYear,
                    cancellationToken);
            }

            if (distinctSlots.Count >= 3)
            {
                await SeedUnderReviewAsync(
                    parent.Id,
                    parentProfile.Id,
                    child1,
                    school,
                    branch,
                    distinctSlots[2],
                    academicYear,
                    cancellationToken);
            }
        }
        else
        {
            logger.LogInformation(
                "Admission baseline apps already exist for parent {Email}; skipping Draft/Submitted/UnderReview insert.",
                ParentEmail);
        }

        await EnsureAcceptedAsync(
            parent.Id,
            parentProfile.Id,
            child2,
            school,
            branch,
            academicYear,
            distinctSlots,
            cancellationToken);

        await EnsureRejectedAsync(
            parent.Id,
            parentProfile.Id,
            child2,
            school,
            branch,
            academicYear,
            distinctSlots,
            cancellationToken);

        await EnsureMissingDocumentsAsync(
            parent.Id,
            parentProfile.Id,
            child2,
            school,
            branch,
            academicYear,
            distinctSlots,
            cancellationToken);

        await EnsureInterviewRequiredAsync(
            parent.Id,
            parentProfile.Id,
            child2,
            school,
            branch,
            academicYear,
            distinctSlots,
            cancellationToken);

        await EnsureWaitingListAsync(
            parent.Id,
            parentProfile.Id,
            child2,
            school,
            branch,
            academicYear,
            distinctSlots,
            cancellationToken);

        await EnsureRegisteredAsync(
            parent.Id,
            parentProfile.Id,
            child2,
            school,
            branch,
            academicYear,
            distinctSlots,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Admission seed completed for parent {Email}.", ParentEmail);
    }

    private async Task<ChildProfile> EnsureChildAsync(
        Guid parentUserId,
        Guid parentProfileId,
        string rawIdentity,
        string fullName,
        DateOnly birthDate,
        ChildGender gender,
        Guid preferredGradeId,
        CancellationToken cancellationToken)
    {
        var normalized = identityProtector.Normalize(rawIdentity);
        var lookupHash = identityProtector.ComputeLookupHash(normalized);

        var existing = await dbContext.ChildProfiles
            .FirstOrDefaultAsync(
                item => item.ParentUserId == parentUserId && item.IdentityLookupHash == lookupHash,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var child = new ChildProfile(
            parentProfileId,
            parentUserId,
            fullName,
            ChildIdentityType.NationalId,
            identityProtector.Protect(normalized),
            lookupHash,
            identityProtector.ExtractLastFour(normalized),
            birthDate,
            gender,
            preferredGradeId,
            hasSpecialNeeds: false,
            specialNeedsNotes: null);

        dbContext.ChildProfiles.Add(child);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Admission seed ensured child {Name} for parent.", fullName);
            return child;
        }
        catch (DbUpdateException)
        {
            // Concurrent host startup can race the unique (ParentUserId, IdentityLookupHash) index.
            foreach (var entry in dbContext.ChangeTracker.Entries<ChildProfile>().ToList())
            {
                if (entry.Entity == child)
                {
                    entry.State = EntityState.Detached;
                }
            }

            var raced = await dbContext.ChildProfiles
                .FirstOrDefaultAsync(
                    item => item.ParentUserId == parentUserId && item.IdentityLookupHash == lookupHash,
                    cancellationToken);

            if (raced is not null)
            {
                logger.LogInformation("Admission seed child {Name} already inserted by concurrent seed.", fullName);
                return raced;
            }

            throw;
        }
    }

    private async Task EnsureAcceptedAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        AcademicYear year,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var hasAccepted = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(
                application =>
                    application.ParentUserId == parentUserId &&
                    application.Status == AdmissionApplicationStatus.Accepted,
                cancellationToken);

        if (hasAccepted)
        {
            return;
        }

        var slot = await FindFreeActiveSlotAsync(child.Id, school.Id, branch.Id, year.Id, slots, cancellationToken);
        if (slot is null)
        {
            logger.LogWarning("Admission Accepted seed skipped; no free active slot for child.");
            return;
        }

        await SeedAcceptedAsync(parentUserId, parentProfileId, child, school, branch, slot, year, cancellationToken);
        logger.LogInformation("Admission Accepted demo application queued.");
    }

    private async Task EnsureRejectedAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        AcademicYear year,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var hasRejected = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(
                application =>
                    application.ParentUserId == parentUserId &&
                    application.Status == AdmissionApplicationStatus.Rejected,
                cancellationToken);

        if (hasRejected)
        {
            return;
        }

        var slot = await FindFreeActiveSlotAsync(child.Id, school.Id, branch.Id, year.Id, slots, cancellationToken)
            ?? slots[0];

        await SeedRejectedAsync(parentUserId, parentProfileId, child, school, branch, slot, year, cancellationToken);
        logger.LogInformation("Admission Rejected demo application queued.");
    }

    private async Task EnsureMissingDocumentsAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        AcademicYear year,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(
                application =>
                    application.ParentUserId == parentUserId &&
                    application.Status == AdmissionApplicationStatus.MissingDocuments,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var slot = await FindFreeActiveSlotAsync(child.Id, school.Id, branch.Id, year.Id, slots, cancellationToken);
        if (slot is null)
        {
            logger.LogWarning("Admission MissingDocuments seed skipped; no free active slot.");
            return;
        }

        var application = await BuildUnderReviewSeedAsync(
            parentUserId,
            parentProfileId,
            child,
            school,
            branch,
            slot,
            year,
            parentNotes: "طلب تجريبي — مستندات ناقصة",
            cancellationToken);

        application.MoveToMissingDocuments();
        var request = new AdmissionMissingItemsRequest(
            application.Id,
            parentVisibleReason: "يرجى تحديث بيانات ولي الأمر الظاهرة",
            instructions: "حدّث الاسم الظاهر ثم أعد الإرسال.",
            responseDeadlineUtc: DateTimeOffset.UtcNow.AddDays(7),
            requestedByUserId: parentUserId);
        request.AddItem(
            new AdmissionMissingItem(
                request.Id,
                AdmissionMissingItemKind.ParentSnapshotField,
                isMandatory: true,
                labelAr: "الاسم الظاهر",
                labelEn: "Display name",
                parentSnapshotField: AdmissionParentSnapshotFieldCode.DisplayName));
        application.AddMissingItemsRequest(request);
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.UnderReview,
                toStatus: AdmissionApplicationStatus.MissingDocuments,
                action: AdmissionHistoryActions.MissingDocumentsRequested,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "مطلوب استكمال معلومات",
                internalNote: "Seed MissingDocuments"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
        logger.LogInformation("Admission MissingDocuments demo application queued.");
    }

    private async Task EnsureInterviewRequiredAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        AcademicYear year,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(
                application =>
                    application.ParentUserId == parentUserId &&
                    application.Status == AdmissionApplicationStatus.InterviewRequired,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var slot = await FindFreeActiveSlotAsync(child.Id, school.Id, branch.Id, year.Id, slots, cancellationToken);
        if (slot is null)
        {
            logger.LogWarning("Admission InterviewRequired seed skipped; no free active slot.");
            return;
        }

        var application = await BuildUnderReviewSeedAsync(
            parentUserId,
            parentProfileId,
            child,
            school,
            branch,
            slot,
            year,
            parentNotes: "طلب تجريبي — مقابلة",
            cancellationToken);

        application.MoveToInterviewRequired();
        var interview = new AdmissionInterviewAppointment(
            application.Id,
            scheduledAtUtc: DateTimeOffset.UtcNow.AddDays(5),
            timeZoneId: "Africa/Cairo",
            mode: AdmissionAppointmentMode.InPerson,
            location: "فرع القاهرة — غرفة المقابلات",
            onlineInstructions: null,
            parentVisibleNotes: "يرجى الحضور قبل الموعد بعشر دقائق.",
            preparationInstructions: "أحضِر بطاقة الهوية.",
            createdByUserId: parentUserId);
        application.AddInterviewAppointment(interview);
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.UnderReview,
                toStatus: AdmissionApplicationStatus.InterviewRequired,
                action: AdmissionHistoryActions.InterviewScheduled,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "تم جدولة مقابلة",
                internalNote: "Seed InterviewRequired"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
        logger.LogInformation("Admission InterviewRequired demo application queued.");
    }

    private async Task EnsureWaitingListAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        AcademicYear year,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(
                application =>
                    application.ParentUserId == parentUserId &&
                    application.Status == AdmissionApplicationStatus.WaitingList,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var slot = await FindFreeActiveSlotAsync(child.Id, school.Id, branch.Id, year.Id, slots, cancellationToken);
        if (slot is null)
        {
            logger.LogWarning("Admission WaitingList seed skipped; no free active slot.");
            return;
        }

        var application = await BuildUnderReviewSeedAsync(
            parentUserId,
            parentProfileId,
            child,
            school,
            branch,
            slot,
            year,
            parentNotes: "طلب تجريبي — قائمة انتظار",
            cancellationToken);

        application.MoveToWaitingList(
            parentVisibleReason: "المقاعد ممتلئة حالياً — تجريبي",
            position: 2,
            reviewDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)));
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.UnderReview,
                toStatus: AdmissionApplicationStatus.WaitingList,
                action: AdmissionHistoryActions.MovedToWaitingList,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "نُقل إلى قائمة الانتظار",
                internalNote: "Seed WaitingList"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
        logger.LogInformation("Admission WaitingList demo application queued.");
    }

    private async Task EnsureRegisteredAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        AcademicYear year,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.AdmissionApplications.AsNoTracking()
            .AnyAsync(
                application =>
                    application.ParentUserId == parentUserId &&
                    application.Status == AdmissionApplicationStatus.Registered,
                cancellationToken);

        if (exists)
        {
            return;
        }

        var slot = await FindFreeActiveSlotAsync(child.Id, school.Id, branch.Id, year.Id, slots, cancellationToken);
        if (slot is null)
        {
            logger.LogWarning("Admission Registered seed skipped; no free active slot.");
            return;
        }

        var application = await BuildUnderReviewSeedAsync(
            parentUserId,
            parentProfileId,
            child,
            school,
            branch,
            slot,
            year,
            parentNotes: "طلب تجريبي — مسجّل",
            cancellationToken);

        application.Accept("Seed acceptance before register");
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.UnderReview,
                toStatus: AdmissionApplicationStatus.Accepted,
                action: AdmissionHistoryActions.Accepted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "تم القبول",
                internalNote: "Seed Accept before Registered"));

        application.MarkRegistered();
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Accepted,
                toStatus: AdmissionApplicationStatus.Registered,
                action: AdmissionHistoryActions.Registered,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "تم تأكيد التسجيل في المنصة",
                internalNote: "Seed Registered (not payment/contract)"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
        logger.LogInformation("Admission Registered demo application queued.");
    }

    private async Task<AdmissionApplication> BuildUnderReviewSeedAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        SeedSlot slot,
        AcademicYear year,
        string parentNotes,
        CancellationToken cancellationToken)
    {
        var (stage, grade) = await LoadStageAndGradeAsync(slot, cancellationToken);
        var number = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            number,
            parentUserId,
            parentProfileId,
            child.Id,
            school.Id,
            branch.Id,
            slot.EducationalStageId,
            slot.GradeId,
            year.Id,
            parentNotes: parentNotes);

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: "Seed lifecycle base"));

        application.Submit(BuildSnapshot(child, school, branch, stage, grade, year));
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Draft,
                toStatus: AdmissionApplicationStatus.Submitted,
                action: AdmissionHistoryActions.Submitted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: "تم إرسال الطلب",
                internalNote: null));

        application.StartReview();
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Submitted,
                toStatus: AdmissionApplicationStatus.UnderReview,
                action: AdmissionHistoryActions.ReviewStarted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "بدأ مراجعة الطلب",
                internalNote: "Seed StartReview"));

        return application;
    }

    private async Task<SeedSlot?> FindFreeActiveSlotAsync(
        Guid childId,
        Guid schoolId,
        Guid branchId,
        Guid yearId,
        IReadOnlyList<SeedSlot> slots,
        CancellationToken cancellationToken)
    {
        var occupiedGradeIds = await dbContext.AdmissionApplications.AsNoTracking()
            .Where(application =>
                application.ChildProfileId == childId &&
                application.SchoolId == schoolId &&
                application.SchoolBranchId == branchId &&
                application.AcademicYearId == yearId &&
                (application.Status == AdmissionApplicationStatus.Draft ||
                 application.Status == AdmissionApplicationStatus.Submitted ||
                 application.Status == AdmissionApplicationStatus.UnderReview ||
                 application.Status == AdmissionApplicationStatus.Accepted ||
                 application.Status == AdmissionApplicationStatus.MissingDocuments ||
                 application.Status == AdmissionApplicationStatus.InterviewRequired ||
                 application.Status == AdmissionApplicationStatus.AssessmentRequired ||
                 application.Status == AdmissionApplicationStatus.WaitingList ||
                 application.Status == AdmissionApplicationStatus.Registered))
            .Select(application => application.GradeId)
            .ToListAsync(cancellationToken);

        var occupied = occupiedGradeIds.ToHashSet();

        // Include applications queued in this DbContext but not yet flushed — otherwise
        // consecutive Ensure* seeds can pick the same active duplicate key.
        foreach (var entry in dbContext.ChangeTracker.Entries<AdmissionApplication>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var application = entry.Entity;
            if (application.ChildProfileId == childId &&
                application.SchoolId == schoolId &&
                application.SchoolBranchId == branchId &&
                application.AcademicYearId == yearId &&
                IsActiveDuplicateSeedStatus(application.Status))
            {
                occupied.Add(application.GradeId);
            }
        }

        return slots.FirstOrDefault(slot => !occupied.Contains(slot.GradeId));
    }

    private static bool IsActiveDuplicateSeedStatus(AdmissionApplicationStatus status) =>
        status is AdmissionApplicationStatus.Draft
            or AdmissionApplicationStatus.Submitted
            or AdmissionApplicationStatus.UnderReview
            or AdmissionApplicationStatus.Accepted
            or AdmissionApplicationStatus.MissingDocuments
            or AdmissionApplicationStatus.InterviewRequired
            or AdmissionApplicationStatus.AssessmentRequired
            or AdmissionApplicationStatus.WaitingList
            or AdmissionApplicationStatus.Registered;

    private async Task SeedDraftAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        SeedSlot slot,
        AcademicYear year,
        CancellationToken cancellationToken)
    {
        var number = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            number,
            parentUserId,
            parentProfileId,
            child.Id,
            school.Id,
            branch.Id,
            slot.EducationalStageId,
            slot.GradeId,
            year.Id,
            parentNotes: "طلب تجريبي — مسودة");

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: "Seed draft"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
    }

    private async Task SeedSubmittedAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        SeedSlot slot,
        AcademicYear year,
        CancellationToken cancellationToken)
    {
        var (stage, grade) = await LoadStageAndGradeAsync(slot, cancellationToken);
        var number = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            number,
            parentUserId,
            parentProfileId,
            child.Id,
            school.Id,
            branch.Id,
            slot.EducationalStageId,
            slot.GradeId,
            year.Id,
            parentNotes: "طلب تجريبي — مُرسل");

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: "Seed submitted"));

        application.Submit(BuildSnapshot(child, school, branch, stage, grade, year));

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Draft,
                toStatus: AdmissionApplicationStatus.Submitted,
                action: AdmissionHistoryActions.Submitted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: "تم إرسال الطلب",
                internalNote: null));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
    }

    private async Task SeedUnderReviewAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        SeedSlot slot,
        AcademicYear year,
        CancellationToken cancellationToken)
    {
        var (stage, grade) = await LoadStageAndGradeAsync(slot, cancellationToken);
        var number = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            number,
            parentUserId,
            parentProfileId,
            child.Id,
            school.Id,
            branch.Id,
            slot.EducationalStageId,
            slot.GradeId,
            year.Id,
            parentNotes: "طلب تجريبي — قيد المراجعة");

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: "Seed under review"));

        application.Submit(BuildSnapshot(child, school, branch, stage, grade, year));

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Draft,
                toStatus: AdmissionApplicationStatus.Submitted,
                action: AdmissionHistoryActions.Submitted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: "تم إرسال الطلب",
                internalNote: null));

        application.StartReview();
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Submitted,
                toStatus: AdmissionApplicationStatus.UnderReview,
                action: AdmissionHistoryActions.ReviewStarted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "بدأ مراجعة الطلب",
                internalNote: "Seed StartReview"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
    }

    private async Task SeedAcceptedAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        SeedSlot slot,
        AcademicYear year,
        CancellationToken cancellationToken)
    {
        var (stage, grade) = await LoadStageAndGradeAsync(slot, cancellationToken);
        var number = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            number,
            parentUserId,
            parentProfileId,
            child.Id,
            school.Id,
            branch.Id,
            slot.EducationalStageId,
            slot.GradeId,
            year.Id,
            parentNotes: "طلب تجريبي — مقبول");

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: "Seed accepted"));

        application.Submit(BuildSnapshot(child, school, branch, stage, grade, year));
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Draft,
                toStatus: AdmissionApplicationStatus.Submitted,
                action: AdmissionHistoryActions.Submitted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: "تم إرسال الطلب",
                internalNote: null));

        application.StartReview();
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Submitted,
                toStatus: AdmissionApplicationStatus.UnderReview,
                action: AdmissionHistoryActions.ReviewStarted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "بدأ مراجعة الطلب",
                internalNote: "Seed StartReview"));

        application.Accept("Seed acceptance note");
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.UnderReview,
                toStatus: AdmissionApplicationStatus.Accepted,
                action: AdmissionHistoryActions.Accepted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "تم قبول الطلب",
                internalNote: "Seed Accept"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
    }

    private async Task SeedRejectedAsync(
        Guid parentUserId,
        Guid parentProfileId,
        ChildProfile child,
        School school,
        SchoolBranch branch,
        SeedSlot slot,
        AcademicYear year,
        CancellationToken cancellationToken)
    {
        var (stage, grade) = await LoadStageAndGradeAsync(slot, cancellationToken);
        var number = await numberGenerator.GenerateAsync(cancellationToken);
        var application = new AdmissionApplication(
            number,
            parentUserId,
            parentProfileId,
            child.Id,
            school.Id,
            branch.Id,
            slot.EducationalStageId,
            slot.GradeId,
            year.Id,
            parentNotes: "طلب تجريبي — مرفوض");

        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: null,
                toStatus: AdmissionApplicationStatus.Draft,
                action: AdmissionHistoryActions.Created,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: null,
                internalNote: "Seed rejected"));

        application.Submit(BuildSnapshot(child, school, branch, stage, grade, year));
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Draft,
                toStatus: AdmissionApplicationStatus.Submitted,
                action: AdmissionHistoryActions.Submitted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.Parent,
                parentVisible: true,
                parentVisibleNote: "تم إرسال الطلب",
                internalNote: null));

        application.StartReview();
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.Submitted,
                toStatus: AdmissionApplicationStatus.UnderReview,
                action: AdmissionHistoryActions.ReviewStarted,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "بدأ مراجعة الطلب",
                internalNote: "Seed StartReview"));

        application.Reject("مستندات غير مكتملة — تجريبي", "Seed rejection note");
        application.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus: AdmissionApplicationStatus.UnderReview,
                toStatus: AdmissionApplicationStatus.Rejected,
                action: AdmissionHistoryActions.Rejected,
                actorUserId: parentUserId,
                actorRole: SchooleraRoles.SchoolAdmin,
                parentVisible: true,
                parentVisibleNote: "تم رفض الطلب",
                internalNote: "Seed Reject"));

        await dbContext.AdmissionApplications.AddAsync(application, cancellationToken);
    }

    private async Task<(EducationalStage Stage, Grade Grade)> LoadStageAndGradeAsync(
        SeedSlot slot,
        CancellationToken cancellationToken)
    {
        var stage = await dbContext.EducationalStages
            .FirstAsync(item => item.Id == slot.EducationalStageId, cancellationToken);
        var grade = await dbContext.Grades
            .FirstAsync(item => item.Id == slot.GradeId, cancellationToken);
        return (stage, grade);
    }

    private static AdmissionSubmissionSnapshot BuildSnapshot(
        ChildProfile child,
        School school,
        SchoolBranch branch,
        EducationalStage stage,
        Grade grade,
        AcademicYear year) =>
        new(
            child.FullName,
            child.CurrentSchoolName,
            child.PreferredStudyLanguage is { } language ? (int)language : null,
            child.Skills,
            child.Hobbies,
            child.Strengths,
            child.ImprovementAreas,
            child.HasSpecialNeeds,
            child.SpecialNeedsNotes,
            school.NameAr,
            school.NameEn,
            branch.NameAr,
            branch.NameEn,
            stage.NameAr,
            stage.NameEn,
            grade.NameAr,
            grade.NameEn,
            year.NameAr,
            year.NameEn,
            ParentDisplayName: "Parent Demo",
            ParentEmail: ParentEmail,
            ParentPhone: "+201000000001",
            ParentAlternatePhone: null,
            FatherFullName: null,
            FatherPhone: null,
            FatherEmail: null,
            FatherOccupation: null,
            FatherQualification: null,
            FatherMaskedIdentity: null,
            MotherFullName: null,
            MotherPhone: null,
            MotherEmail: null,
            MotherOccupation: null,
            MotherQualification: null,
            MotherMaskedIdentity: null);

    private sealed record SeedSlot(Guid EducationalStageId, Guid GradeId);
}