using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.SchoolPortal.Commands.CreateSchoolStageOffering;

public sealed record CreateSchoolStageOfferingCommand(
    Guid SchoolId,
    CreateSchoolStageOfferingRequest Body) : IRequest<Result<SchoolStageOfferingDto>>;

public sealed class CreateSchoolStageOfferingCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<CreateSchoolStageOfferingCommandHandler> logger)
    : IRequestHandler<CreateSchoolStageOfferingCommand, Result<SchoolStageOfferingDto>>
{
    public async Task<Result<SchoolStageOfferingDto>> Handle(
        CreateSchoolStageOfferingCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolStageOfferingDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolStageOfferingDto>(
            accessResult.Data, SchoolPortalPermission.ManageOfferings, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var body = request.Body;
        var branch = await repository.GetBranchForWriteAsync(request.SchoolId, body.BranchId, cancellationToken);
        if (branch is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.BranchNotFound);
        }

        if (!await repository.EducationalStageExistsAsync(body.EducationalStageId, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.InvalidStage);
        }

        if (!await repository.GradesBelongToStageAsync(body.EducationalStageId, body.GradeIds, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.InvalidGrade);
        }

        if (await repository.DuplicateOfferingExistsAsync(
                body.BranchId, body.EducationalStageId, body.GenderType, null, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.DuplicateOffering);
        }

        var offering = new SchoolStageOffering(
            body.BranchId,
            body.EducationalStageId,
            body.GenderType,
            body.Capacity,
            body.IsAdmissionOpen);

        branch.StageOfferings.Add(offering);

        foreach (var gradeId in body.GradeIds.Distinct())
        {
            offering.AddGradeOffering(new SchoolGradeOffering(offering.Id, gradeId));
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolStageOfferingDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await repository.GetOfferingForWriteAsync(
            request.SchoolId, offering.Id, cancellationToken);
        if (loaded is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.OfferingNotFound);
        }

        return Result<SchoolStageOfferingDto>.Success(SchoolPortalReadModel.ToOffering(loaded));
    }
}
