using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;
using Schoolera.Infrastructure.Identity;

namespace Schoolera.Infrastructure.Persistence;

public sealed class SchoolPortalSeeder(
    SchooleraDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ILogger<SchoolPortalSeeder> logger)
{
    private const string DemoSchoolSlug = "cairo-international-school";
    private const string SchoolAdminEmail = "schooladmin@schoolera.local";
    private const string SchoolOwnerEmail = "schoolowner@schoolera.local";
    private const string AdmissionOfficerEmail = "admissionofficer@schoolera.local";
    private const string FinanceOfficerEmail = "financeofficer@schoolera.local";
    private const string ContentModeratorEmail = "contentmoderator@schoolera.local";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running school portal seed...");
        await SeedAdditionalServicesAsync(cancellationToken);
        await SeedDemoOwnershipAndTeamAsync(cancellationToken);
        logger.LogInformation("School portal seed completed.");
    }

    private async Task SeedAdditionalServicesAsync(CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools
            .FirstOrDefaultAsync(entry => entry.Slug == DemoSchoolSlug, cancellationToken);

        if (school is null)
        {
            logger.LogInformation("Portal demo services skipped; school {Slug} not found.", DemoSchoolSlug);
            return;
        }

        var existingNames = await dbContext.SchoolAdditionalServices
            .Where(service => service.SchoolId == school.Id)
            .Select(service => service.NameAr)
            .ToListAsync(cancellationToken);

        var existing = existingNames.ToHashSet(StringComparer.Ordinal);
        var services = new (string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn, string IconKey, int SortOrder)[]
        {
            ("النقل المدرسي", "School Transport", "خدمة نقل يومية آمنة", "Safe daily transport service", "bus", 1),
            ("وجبات غذائية", "Meals Program", "برنامج وجبات متوازنة", "Balanced meals program", "cafeteria", 2),
            ("أنشطة بعد المدرسة", "After-school Activities", "أنشطة تعليمية ورياضية", "Educational and sports activities", "activities", 3),
        };

        var inserted = 0;
        foreach (var (nameAr, nameEn, descriptionAr, descriptionEn, iconKey, sortOrder) in services)
        {
            if (existing.Contains(nameAr))
            {
                continue;
            }

            dbContext.SchoolAdditionalServices.Add(new SchoolAdditionalService(
                school.Id,
                nameAr,
                nameEn,
                descriptionAr,
                descriptionEn,
                iconKey,
                sortOrder));
            inserted++;
        }

        if (inserted > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Portal demo services inserted={Inserted}.", inserted);
    }

    private async Task SeedDemoOwnershipAndTeamAsync(CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools
            .FirstOrDefaultAsync(entry => entry.Slug == DemoSchoolSlug, cancellationToken);

        if (school is null)
        {
            return;
        }

        var owner = await FindUserByEmailAsync(SchoolOwnerEmail, cancellationToken);
        if (owner is not null && school.OwnerUserId is null)
        {
            school.AssignOwner(owner.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Assigned demo owner to school {Slug}.", DemoSchoolSlug);
        }

        if (owner is null)
        {
            logger.LogInformation("Portal team membership skipped (owner missing).");
            return;
        }

        await EnsureMembershipAsync(
            school.Id,
            owner.Id,
            SchoolAdminEmail,
            SchoolTeamRole.SchoolAdmin,
            cancellationToken);

        await EnsureMembershipAsync(
            school.Id,
            owner.Id,
            AdmissionOfficerEmail,
            SchoolTeamRole.AdmissionOfficer,
            cancellationToken);

        await EnsureMembershipAsync(
            school.Id,
            owner.Id,
            FinanceOfficerEmail,
            SchoolTeamRole.FinanceOfficer,
            cancellationToken);

        await EnsureMembershipAsync(
            school.Id,
            owner.Id,
            ContentModeratorEmail,
            SchoolTeamRole.ContentModerator,
            cancellationToken);
    }

    private async Task EnsureMembershipAsync(
        Guid schoolId,
        Guid ownerUserId,
        string email,
        SchoolTeamRole role,
        CancellationToken cancellationToken)
    {
        var user = await FindUserByEmailAsync(email, cancellationToken);
        if (user is null || user.Id == ownerUserId)
        {
            return;
        }

        var exists = await dbContext.SchoolTeamMembers
            .AnyAsync(
                member => member.SchoolId == schoolId &&
                          member.UserId == user.Id &&
                          member.IsActive,
                cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.SchoolTeamMembers.Add(new SchoolTeamMember(
            schoolId,
            user.Id,
            role,
            ownerUserId,
            SchoolBranchScopeMode.AllBranches));

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Role} membership for {Email}.", role, email);
    }

    private async Task<ApplicationUser?> FindUserByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalized = userManager.NormalizeEmail(email);
        return await dbContext.Users
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalized, cancellationToken);
    }
}
