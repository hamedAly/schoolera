using System.Globalization;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Schools.Queries.GetPublicAdmissionRequirements;

public sealed record GetPublicAdmissionRequirementsQuery(
    string Slug,
    Guid? BranchId,
    Guid? EducationalStageId,
    Guid? GradeId,
    Guid? AcademicYearId) : IRequest<Result<IReadOnlyList<PublicAdmissionRequirementSummaryDto>>>;

public sealed class GetPublicAdmissionRequirementsQueryHandler(
    ISchoolReadRepository schoolReadRepository,
    ISchoolAdmissionRequirementRepository requirementRepository,
    ILogger<GetPublicAdmissionRequirementsQueryHandler> logger)
    : IRequestHandler<GetPublicAdmissionRequirementsQuery, Result<IReadOnlyList<PublicAdmissionRequirementSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<PublicAdmissionRequirementSummaryDto>>> Handle(
        GetPublicAdmissionRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogDebug("Public admission requirements for slug {Slug}.", request.Slug);

        var school = await schoolReadRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);
        if (school is null)
        {
            return Result<IReadOnlyList<PublicAdmissionRequirementSummaryDto>>.Failure(
                ["School not found."],
                [SchoolErrorCodes.NotFound]);
        }

        var published = await requirementRepository.ListPublishedActiveAsync(school.Id, cancellationToken);

        IReadOnlyList<Domain.Entities.SchoolAdmissionRequirement> applicable;
        if (request.BranchId is { } branchId &&
            request.EducationalStageId is { } stageId &&
            request.GradeId is { } gradeId &&
            request.AcademicYearId is { } yearId)
        {
            applicable = AdmissionRequirementCatalog.ResolveApplicable(
                published, branchId, stageId, gradeId, yearId);
        }
        else
        {
            // Partial filters: parent-safe school-default published definitions only.
            applicable = published
                .Where(requirement =>
                    requirement.SchoolBranchId is null &&
                    requirement.EducationalStageId is null &&
                    requirement.GradeId is null &&
                    requirement.AcademicYearId is null)
                .GroupBy(requirement => requirement.RequirementCode, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderBy(item => item.Id).First())
                .OrderBy(requirement => requirement.SortOrder)
                .ThenBy(requirement => requirement.RequirementCode)
                .ToArray();
        }

        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .Equals("ar", StringComparison.OrdinalIgnoreCase);

        return Result<IReadOnlyList<PublicAdmissionRequirementSummaryDto>>.Success(
            applicable
                .Select(requirement => new PublicAdmissionRequirementSummaryDto(
                    requirement.RequirementCode,
                    requirement.Kind,
                    isArabic ? requirement.NameAr : requirement.NameEn,
                    isArabic ? requirement.DescriptionAr : requirement.DescriptionEn,
                    requirement.IsRequired,
                    requirement.SortOrder,
                    requirement.DocumentCode,
                    AdmissionRequirementCatalog.ParseExtensions(requirement.AllowedFileExtensions),
                    requirement.MaxFileSizeBytes,
                    requirement.AllowChildVaultCopy))
                .ToArray());
    }
}