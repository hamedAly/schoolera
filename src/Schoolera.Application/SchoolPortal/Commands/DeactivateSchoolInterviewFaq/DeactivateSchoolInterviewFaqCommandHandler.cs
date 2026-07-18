using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.DeactivateSchoolInterviewFaq;

public sealed record DeactivateSchoolInterviewFaqCommand(Guid SchoolId, Guid ItemId)
    : IRequest<Result<SchoolInterviewFaqDetailDto>>;

public sealed class DeactivateSchoolInterviewFaqCommandHandler(
    ISchoolPortalAccess portalAccess,
    ICmsRepository cmsRepository,
    ISchoolPortalAuditWriter auditWriter,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<DeactivateSchoolInterviewFaqCommandHandler> logger)
    : IRequestHandler<DeactivateSchoolInterviewFaqCommand, Result<SchoolInterviewFaqDetailDto>>
{
    public async Task<Result<SchoolInterviewFaqDetailDto>> Handle(
        DeactivateSchoolInterviewFaqCommand request,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await SchoolInterviewFaqSupport.ResolveWriteAccessAsync(
            portalAccess, request.SchoolId, localizer, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var item = await cmsRepository.GetSchoolFaqItemAsync(request.SchoolId, request.ItemId, cancellationToken);
        if (item is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolInterviewFaqDetailDto>(
                localizer, SchoolPortalErrorCodes.InterviewFaqNotFound);
        }

        item.Deactivate();

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolInterviewFaqDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await auditWriter.WriteAsync(
            access!.UserId,
            SchoolPortalAuditActions.InterviewFaqDeactivated,
            "FaqItem",
            item.Id.ToString(),
            "Deactivated school interview FAQ.",
            cancellationToken);

        logger.LogInformation("Deactivated school interview FAQ {ItemId}.", item.Id);
        return Result<SchoolInterviewFaqDetailDto>.Success(SchoolInterviewFaqSupport.ToDetail(item));
    }
}
