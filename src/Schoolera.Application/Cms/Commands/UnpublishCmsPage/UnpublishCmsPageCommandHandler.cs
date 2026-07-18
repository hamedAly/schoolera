using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.UnpublishCmsPage;

public sealed record UnpublishCmsPageCommand(Guid Id) : IRequest<Result<CmsPageAdminDto>>;

public sealed class UnpublishCmsPageCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UnpublishCmsPageCommandHandler> logger)
    : IRequestHandler<UnpublishCmsPageCommand, Result<CmsPageAdminDto>>
{
    public async Task<Result<CmsPageAdminDto>> Handle(
        UnpublishCmsPageCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<CmsPageAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var page = await cmsRepository.GetPageByIdAsync(request.Id, cancellationToken);
        if (page is null)
        {
            return Result<CmsPageAdminDto>.Failure(
                ["CMS page not found."],
                [CmsErrorCodes.PageNotFound]);
        }

        page.Unpublish();

        var conflict = await CmsResults.TrySaveAsync<CmsPageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsPageUnpublished,
            "CmsPage",
            page.Id.ToString(),
            $"Unpublished CMS page '{page.Slug}'.",
            cancellationToken);

        logger.LogInformation("Unpublished CMS page {PageId}.", page.Id);

        return Result<CmsPageAdminDto>.Success(CmsPageAdminDto.FromEntity(page));
    }
}
