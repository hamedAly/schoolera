using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.ArchiveCmsPage;

public sealed record ArchiveCmsPageCommand(Guid Id) : IRequest<Result<CmsPageAdminDto>>;

public sealed class ArchiveCmsPageCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<ArchiveCmsPageCommandHandler> logger)
    : IRequestHandler<ArchiveCmsPageCommand, Result<CmsPageAdminDto>>
{
    public async Task<Result<CmsPageAdminDto>> Handle(
        ArchiveCmsPageCommand request,
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

        page.Archive();

        var conflict = await CmsResults.TrySaveAsync<CmsPageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsPageArchived,
            "CmsPage",
            page.Id.ToString(),
            $"Archived CMS page '{page.Slug}'.",
            cancellationToken);

        logger.LogInformation("Archived CMS page {PageId}.", page.Id);

        return Result<CmsPageAdminDto>.Success(CmsPageAdminDto.FromEntity(page));
    }
}
