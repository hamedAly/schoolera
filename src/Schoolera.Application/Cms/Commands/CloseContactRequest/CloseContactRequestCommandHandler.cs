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

namespace Schoolera.Application.Cms.Commands.CloseContactRequest;

public sealed record CloseContactRequestCommand(Guid Id, string? AdminNote)
    : IRequest<Result<ContactRequestDetailDto>>;

public sealed class CloseContactRequestCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<CloseContactRequestCommandHandler> logger)
    : IRequestHandler<CloseContactRequestCommand, Result<ContactRequestDetailDto>>
{
    public async Task<Result<ContactRequestDetailDto>> Handle(
        CloseContactRequestCommand request,
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

        if (contactRequest.Status == ContactRequestStatus.Closed)
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Invalid contact request status transition."],
                [ContactErrorCodes.InvalidTransition]);
        }

        try
        {
            contactRequest.Close(actorId, request.AdminNote);
        }
        catch (InvalidOperationException)
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Invalid contact request status transition."],
                [ContactErrorCodes.InvalidTransition]);
        }

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
            $"Closed contact request {contactRequest.Reference}.",
            cancellationToken);

        logger.LogInformation(
            "Closed contact request {ContactRequestId}.",
            contactRequest.Id);

        return Result<ContactRequestDetailDto>.Success(CmsDtoMapping.ToDetail(contactRequest));
    }
}
