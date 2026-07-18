using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Common;

public sealed class AdmissionEligibilityService(
    IParentProfileRepository parentProfileRepository,
    IChildProfileRepository childProfileRepository,
    ISchoolReadRepository schoolReadRepository,
    ISchoolPortalRepository schoolPortalRepository,
    ITaxonomyRepository taxonomyRepository) : IAdmissionEligibilityService
{
    public async Task<Result<AdmissionEligibilityContext>> ValidateAsync(
        AdmissionEligibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var child = await childProfileRepository.GetOwnedAsync(
            request.ParentUserId,
            request.ChildProfileId,
            cancellationToken);
        if (child is null)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Child not found.",
                AdmissionErrorCodes.StudentNotOwned);
        }

        if (request.RequireActiveChild && !child.IsActive)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Child is inactive.",
                AdmissionErrorCodes.StudentInactive);
        }

        var parentProfile = await parentProfileRepository.GetByUserIdAsync(
            request.ParentUserId,
            cancellationToken);
        if (parentProfile is null)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Parent profile is incomplete.",
                AdmissionErrorCodes.Incomplete);
        }

        var school = await ResolveSchoolAsync(request, cancellationToken);
        if (school is null || school.Status != SchoolStatus.Published)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "School is not available for admission.",
                AdmissionErrorCodes.SchoolNotAvailable);
        }

        // Use AsNoTracking branch reads. GetBranchForWriteAsync left the branch tracked in the
        // same DbContext as a RowVersion-protected AdmissionApplication and caused false
        // ConcurrentUpdate failures on submit/cancel SaveChanges.
        var branch = (await schoolPortalRepository.ListBranchesAsync(school.Id, cancellationToken))
            .FirstOrDefault(item => item.Id == request.SchoolBranchId);
        if (branch is null || branch.SchoolId != school.Id || !branch.IsActive)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Branch is not available for admission.",
                AdmissionErrorCodes.BranchNotAvailable);
        }

        var stage = await taxonomyRepository.GetEducationalStageByIdAsync(
            request.EducationalStageId,
            cancellationToken);
        if (stage is null || !stage.IsActive)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Stage and grade combination is invalid.",
                AdmissionErrorCodes.InvalidStageGrade);
        }

        var grade = await taxonomyRepository.GetGradeByIdAsync(request.GradeId, cancellationToken);
        if (grade is null || !grade.IsActive || grade.EducationalStageId != stage.Id)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Stage and grade combination is invalid.",
                AdmissionErrorCodes.InvalidStageGrade);
        }

        var academicYear = await taxonomyRepository.GetAcademicYearByIdAsync(
            request.AcademicYearId,
            cancellationToken);
        if (academicYear is null || !academicYear.IsActive)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Academic year is invalid.",
                AdmissionErrorCodes.InvalidAcademicYear);
        }

        var offerings = await schoolPortalRepository.ListOfferingsAsync(
            school.Id,
            branch.Id,
            cancellationToken);

        var matchingOfferings = offerings
            .Where(offering =>
                offering.EducationalStageId == stage.Id &&
                offering.IsActive &&
                offering.IsAdmissionOpen &&
                IsGenderEligible(child.Gender, offering.GenderType))
            .ToList();

        if (matchingOfferings.Count == 0)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Admission is closed for the selected stage.",
                AdmissionErrorCodes.AdmissionClosed);
        }

        if (!IsGenderEligible(child.Gender, school.GenderType))
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Child gender is not eligible for this school.",
                AdmissionErrorCodes.GenderNotEligible);
        }

        SchoolStageOffering? stageOffering = null;
        SchoolGradeOffering? gradeOffering = null;
        foreach (var offering in matchingOfferings)
        {
            var gradeMatch = offering.GradeOfferings.FirstOrDefault(item =>
                item.GradeId == grade.Id && item.IsActive);
            if (gradeMatch is null)
            {
                continue;
            }

            stageOffering = offering;
            gradeOffering = gradeMatch;
            break;
        }

        if (stageOffering is null || gradeOffering is null)
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Stage and grade combination is invalid.",
                AdmissionErrorCodes.InvalidStageGrade);
        }

        if (!IsGenderEligible(child.Gender, stageOffering.GenderType))
        {
            return AdmissionResults.Failure<AdmissionEligibilityContext>(
                "Child gender is not eligible for this offering.",
                AdmissionErrorCodes.GenderNotEligible);
        }

        return Result<AdmissionEligibilityContext>.Success(
            new AdmissionEligibilityContext(
                parentProfile,
                child,
                school,
                branch,
                stage,
                grade,
                academicYear,
                stageOffering,
                gradeOffering));
    }

    private async Task<School?> ResolveSchoolAsync(
        AdmissionEligibilityRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SchoolId is { } schoolId)
        {
            return await schoolPortalRepository.GetSchoolProfileAsync(schoolId, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.SchoolSlug))
        {
            return await schoolReadRepository.GetPublishedBySlugAsync(
                request.SchoolSlug.Trim(),
                cancellationToken);
        }

        return null;
    }

    private static bool IsGenderEligible(ChildGender childGender, GenderType genderType) =>
        childGender switch
        {
            ChildGender.Male => genderType is GenderType.Boys or GenderType.Mixed,
            ChildGender.Female => genderType is GenderType.Girls or GenderType.Mixed,
            _ => false,
        };
}
