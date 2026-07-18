using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Queries.ListSchoolAdmissionQuestions;

public sealed record ListSchoolAdmissionQuestionsQuery(
    Guid SchoolId,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId,
    AdmissionQuestionType? QuestionType,
    AdmissionQuestionPublicationStatus? PublicationStatus,
    bool? IsActive) : IRequest<Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>>
{
    public static ListSchoolAdmissionQuestionsQuery FromFilters(
        Guid schoolId,
        Guid? branchId,
        Guid? educationalStageId,
        Guid? gradeId,
        Guid? academicYearId,
        int? questionType,
        int? publicationStatus,
        bool? isActive) =>
        new(
            schoolId,
            branchId,
            educationalStageId,
            gradeId,
            academicYearId,
            questionType is { } typeValue ? (AdmissionQuestionType)typeValue : null,
            publicationStatus is { } statusValue
                ? (AdmissionQuestionPublicationStatus)statusValue
                : null,
            isActive);
}

public sealed class ListSchoolAdmissionQuestionsQueryHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolAdmissionQuestionRepository questionRepository,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<ListSchoolAdmissionQuestionsQuery, Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>>
{
    public async Task<Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>> Handle(
        ListSchoolAdmissionQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!access.Succeeded)
        {
            return Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>.Failure(
                access.Errors, access.ErrorCodes);
        }

        
        var permissionCheck = SchoolPortalAccess.RequirePermission<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>(
            access.Data!, SchoolPortalPermission.ManageAdmissionQuestions, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

var items = await questionRepository.ListAsync(
            request.SchoolId,
            request.BranchId,
            request.EducationalStageId,
            request.GradeId,
            request.AcademicYearId,
            request.QuestionType,
            request.PublicationStatus,
            request.IsActive,
            cancellationToken);

        return Result<IReadOnlyList<SchoolAdmissionQuestionListItemDto>>.Success(
            items.Select(SchoolAdmissionQuestionMapping.ToListItem).ToArray());
    }
}
