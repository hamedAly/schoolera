using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.UnpublishHomepageContent;

public sealed record UnpublishHomepageContentCommand(Guid Id) : IRequest<Result<HomepageAdminDto>>;

public sealed class UnpublishHomepageContentCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UnpublishHomepageContentCommandHandler> logger)
    : IRequestHandler<UnpublishHomepageContentCommand, Result<HomepageAdminDto>>
{
    public async Task<Result<HomepageAdminDto>> Handle(
        UnpublishHomepageContentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var content = await cmsRepository.GetHomepageByIdAsync(request.Id, cancellationToken);
        if (content is null)
        {
            return Result<HomepageAdminDto>.Failure(
                ["Homepage content not found."],
                [CmsErrorCodes.HomeNotFound]);
        }

        content.Unpublish();

        var conflict = await CmsResults.TrySaveAsync<HomepageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsHomeUnpublished,
            "HomepageContent",
            content.Id.ToString(),
            "Unpublished homepage content.",
            cancellationToken);

        logger.LogInformation("Unpublished homepage content {ContentId}.", content.Id);

        return Result<HomepageAdminDto>.Success(HomepageAdminDto.FromEntity(content));
    }
}
