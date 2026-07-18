using Microsoft.Extensions.Localization;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Common;

internal static class SchoolInterviewFaqSupport
{
    public static async Task<(SchoolPortalAccessContext? Access, Result<SchoolInterviewFaqDetailDto>? Failure)>
        ResolveWriteAccessAsync(
            ISchoolPortalAccess portalAccess,
            Guid schoolId,
            IStringLocalizer<SchoolPortalMessages> localizer,
            CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(schoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return (null, Result<SchoolInterviewFaqDetailDto>.Failure(access.Errors, access.ErrorCodes));
        }

        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolInterviewFaqDetailDto>(
            access.Data, SchoolPortalPermission.ManageContent, localizer);
        if (!permissionCheck.Succeeded)
        {
            return (null, permissionCheck);
        }

        return (access.Data, null);
    }

    public static async Task<(SchoolPortalAccessContext? Access, Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>? Failure)>
        ResolveReadAccessAsync(
            ISchoolPortalAccess portalAccess,
            Guid schoolId,
            IStringLocalizer<SchoolPortalMessages> localizer,
            CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(schoolId, cancellationToken);
        if (!access.Succeeded || access.Data is null)
        {
            return (null, Result<IReadOnlyList<SchoolInterviewFaqDetailDto>>.Failure(access.Errors, access.ErrorCodes));
        }

        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolInterviewFaqDetailDto>>(
            access.Data, SchoolPortalPermission.ManageContent, localizer);
        if (!permissionCheck.Succeeded)
        {
            return (null, permissionCheck);
        }

        return (access.Data, null);
    }

    public static async Task<string?> ValidateApplicabilityAsync(
        ISchoolPortalRepository portalRepository,
        Guid schoolId,
        Guid? branchId,
        Guid? stageId,
        Guid? gradeId,
        Guid? yearId,
        CancellationToken cancellationToken)
    {
        if (branchId is { } bid)
        {
            var branch = await portalRepository.GetBranchForWriteAsync(schoolId, bid, cancellationToken);
            if (branch is null)
            {
                return SchoolPortalErrorCodes.BranchNotFound;
            }
        }

        if (stageId is { } sid)
        {
            if (!await portalRepository.EducationalStageExistsAsync(sid, cancellationToken))
            {
                return SchoolPortalErrorCodes.InvalidStage;
            }
        }

        if (gradeId is { } gid)
        {
            if (stageId is null)
            {
                return SchoolPortalErrorCodes.InvalidGrade;
            }

            if (!await portalRepository.GradesBelongToStageAsync(stageId.Value, [gid], cancellationToken))
            {
                return SchoolPortalErrorCodes.InvalidGrade;
            }
        }

        if (yearId is { } yid)
        {
            if (!await portalRepository.AcademicYearExistsAsync(yid, cancellationToken))
            {
                return SchoolPortalErrorCodes.InvalidAcademicYear;
            }
        }

        return null;
    }

    public static async Task<FaqCategory> EnsureInterviewCategoryAsync(
        ICmsRepository cmsRepository,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var category = await cmsRepository.GetFaqCategoryBySlugAsync(
            CmsSlugs.InterviewFaqCategorySlug,
            cancellationToken);
        if (category is not null)
        {
            return category;
        }

        var sortOrder = await cmsRepository.GetNextFaqCategorySortOrderAsync(cancellationToken);
        category = new FaqCategory(
            "أسئلة المقابلة والتقييم",
            "Interview and assessment FAQs",
            CmsSlugs.InterviewFaqCategorySlug,
            sortOrder);
        category.Publish();
        await cmsRepository.AddFaqCategoryAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category;
    }

    public static SchoolInterviewFaqDetailDto ToDetail(FaqItem item) =>
        SchoolInterviewFaqDetailDto.FromAdmin(FaqItemAdminDto.FromEntity(item));
}
