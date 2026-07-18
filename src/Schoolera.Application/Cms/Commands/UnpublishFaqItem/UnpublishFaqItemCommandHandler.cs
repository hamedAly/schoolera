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

namespace Schoolera.Application.Cms.Commands.UnpublishFaqItem;

public sealed record UnpublishFaqItemCommand(Guid Id) : IRequest<Result<FaqItemAdminDto>>;

public sealed class UnpublishFaqItemCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UnpublishFaqItemCommandHandler> logger)
    : IRequestHandler<UnpublishFaqItemCommand, Result<FaqItemAdminDto>>
{
    public async Task<Result<FaqItemAdminDto>> Handle(
        UnpublishFaqItemCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqItemAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var item = await cmsRepository.GetFaqItemByIdAsync(request.Id, cancellationToken);
        if (item is null || item.OwnershipScope != FaqOwnershipScope.Platform)
        {
            return Result<FaqItemAdminDto>.Failure(
                ["FAQ item not found."],
                [CmsErrorCodes.FaqNotFound]);
        }

        item.Unpublish();

        var conflict = await CmsResults.TrySaveAsync<FaqItemAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqItemUnpublished,
            "FaqItem",
            item.Id.ToString(),
            "Unpublished FAQ item.",
            cancellationToken);

        logger.LogInformation("Unpublished FAQ item {ItemId}.", item.Id);

        return Result<FaqItemAdminDto>.Success(FaqItemAdminDto.FromEntity(item));
    }
}
