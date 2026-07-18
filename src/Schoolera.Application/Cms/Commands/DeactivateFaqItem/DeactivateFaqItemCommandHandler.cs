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

namespace Schoolera.Application.Cms.Commands.DeactivateFaqItem;

public sealed record DeactivateFaqItemCommand(Guid Id) : IRequest<Result<FaqItemAdminDto>>;

public sealed class DeactivateFaqItemCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateFaqItemCommandHandler> logger)
    : IRequestHandler<DeactivateFaqItemCommand, Result<FaqItemAdminDto>>
{
    public async Task<Result<FaqItemAdminDto>> Handle(
        DeactivateFaqItemCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<FaqItemAdminDto>.Failure(["Forbidden."], [CmsErrorCodes.Forbidden]);
        }

        var item = await cmsRepository.GetFaqItemByIdForUpdateAsync(request.Id, cancellationToken);
        if (item is null || item.OwnershipScope != FaqOwnershipScope.Platform)
        {
            return Result<FaqItemAdminDto>.Failure(["FAQ item not found."], [CmsErrorCodes.FaqNotFound]);
        }

        item.Deactivate();

        var conflict = await CmsResults.TrySaveAsync<FaqItemAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsFaqItemDeactivated,
            "FaqItem",
            item.Id.ToString(),
            "Deactivated FAQ item.",
            cancellationToken);

        logger.LogInformation("Deactivated FAQ item {ItemId}.", item.Id);
        return Result<FaqItemAdminDto>.Success(FaqItemAdminDto.FromEntity(item));
    }
}
