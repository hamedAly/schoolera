using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Cms.Commands.StartContactRequestReview;

public sealed record StartContactRequestReviewCommand(Guid Id, string? AdminNote)
    : IRequest<Result<ContactRequestDetailDto>>;

public sealed class StartContactRequestReviewCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<StartContactRequestReviewCommandHandler> logger)
    : IRequestHandler<StartContactRequestReviewCommand, Result<ContactRequestDetailDto>>
{
    public async Task<Result<ContactRequestDetailDto>> Handle(
        StartContactRequestReviewCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var contactRequest = await cmsRepository.GetContactRequestByIdAsync(request.Id, cancellationToken);
        if (contactRequest is null)
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Contact request not found."],
                [ContactErrorCodes.NotFound]);
        }

        if (contactRequest.Status != ContactRequestStatus.New)
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Invalid contact request status transition."],
                [ContactErrorCodes.InvalidTransition]);
        }

        contactRequest.StartReview(actorId, request.AdminNote);

        var conflict = await CmsResults.TrySaveAsync<ContactRequestDetailDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.ContactStatusChanged,
            "ContactRequest",
            contactRequest.Id.ToString(),
            $"Started review of contact request {contactRequest.Reference}.",
            cancellationToken);

        logger.LogInformation(
            "Started review of contact request {ContactRequestId}.",
            contactRequest.Id);

        return Result<ContactRequestDetailDto>.Success(CmsDtoMapping.ToDetail(contactRequest));
    }
}
