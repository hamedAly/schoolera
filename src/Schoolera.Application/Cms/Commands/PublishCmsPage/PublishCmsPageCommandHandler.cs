using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.PublishCmsPage;

public sealed record PublishCmsPageCommand(Guid Id) : IRequest<Result<CmsPageAdminDto>>;

public sealed class PublishCmsPageCommandHandler(
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    ILegalConsentService legalConsentService,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<PublishCmsPageCommandHandler> logger)
    : IRequestHandler<PublishCmsPageCommand, Result<CmsPageAdminDto>>
{
    public async Task<Result<CmsPageAdminDto>> Handle(
        PublishCmsPageCommand request,
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

        var sanitizedContentAr = contentSanitizer.SanitizeHtml(page.ContentAr);
        var sanitizedContentEn = contentSanitizer.SanitizeHtml(page.ContentEn);
        page.UpdateContent(
            page.TitleAr,
            page.TitleEn,
            sanitizedContentAr,
            sanitizedContentEn,
            page.MetaTitleAr,
            page.MetaTitleEn,
            page.MetaDescriptionAr,
            page.MetaDescriptionEn);

        if (!CmsPublishRules.HasPublishableBilingualContent(
                page.TitleAr,
                page.TitleEn,
                page.ContentAr,
                page.ContentEn))
        {
            return Result<CmsPageAdminDto>.Failure(
                ["Arabic and English title and content are required to publish."],
                [CmsErrorCodes.PagePublishValidation]);
        }

        page.Publish();

        if (page.Slug is "terms" or "privacy")
        {
            await legalConsentService.PublishVersionsFromCmsPageAsync(
                page.Slug,
                page.TitleAr,
                page.TitleEn,
                page.ContentAr,
                page.ContentEn,
                page.PublishedAtUtc ?? DateTimeOffset.UtcNow,
                cancellationToken);
        }

        var conflict = await CmsResults.TrySaveAsync<CmsPageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsPagePublished,
            "CmsPage",
            page.Id.ToString(),
            $"Published CMS page '{page.Slug}'.",
            cancellationToken);

        logger.LogInformation("Published CMS page {PageId}.", page.Id);

        return Result<CmsPageAdminDto>.Success(CmsPageAdminDto.FromEntity(page));
    }
}
