using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Cms.Commands.CreateCmsPage;

public sealed record CreateCmsPageCommand(
    string Slug,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    string? MetaTitleAr,
    string? MetaTitleEn,
    string? MetaDescriptionAr,
    string? MetaDescriptionEn) : IRequest<Result<CmsPageAdminDto>>;

public sealed class CreateCmsPageCommandHandler(
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<CreateCmsPageCommandHandler> logger)
    : IRequestHandler<CreateCmsPageCommand, Result<CmsPageAdminDto>>
{
    public async Task<Result<CmsPageAdminDto>> Handle(
        CreateCmsPageCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<CmsPageAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        if (!CmsSlugValidator.IsValidCustomSlug(request.Slug))
        {
            var errorCode = CmsSlugs.IsReserved(request.Slug)
                ? CmsErrorCodes.PageSlugReserved
                : CmsErrorCodes.PageSlugInvalid;

            return Result<CmsPageAdminDto>.Failure(
                ["Invalid CMS page slug."],
                [errorCode]);
        }

        if (await cmsRepository.IsPageSlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<CmsPageAdminDto>.Failure(
                ["CMS page slug is already taken."],
                [CmsErrorCodes.PageSlugDuplicate]);
        }

        var page = new CmsPage(
            request.Slug,
            request.TitleAr,
            request.TitleEn,
            contentSanitizer.SanitizeHtml(request.ContentAr),
            contentSanitizer.SanitizeHtml(request.ContentEn),
            request.MetaTitleAr,
            request.MetaTitleEn,
            request.MetaDescriptionAr,
            request.MetaDescriptionEn,
            isSystemPage: false);

        await cmsRepository.AddPageAsync(page, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsPageCreated,
            "CmsPage",
            page.Id.ToString(),
            $"Created CMS page '{page.Slug}'.",
            cancellationToken);

        logger.LogInformation("Created CMS page {PageId} with slug {Slug}.", page.Id, page.Slug);

        return Result<CmsPageAdminDto>.Success(CmsPageAdminDto.FromEntity(page));
    }
}
