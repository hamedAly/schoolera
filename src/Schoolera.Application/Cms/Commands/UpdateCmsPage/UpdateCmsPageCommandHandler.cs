using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.UpdateCmsPage;

public sealed record UpdateCmsPageBody(
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    string? MetaTitleAr,
    string? MetaTitleEn,
    string? MetaDescriptionAr,
    string? MetaDescriptionEn,
    string? Slug,
    byte[] RowVersion);

public sealed record UpdateCmsPageCommand(Guid Id, UpdateCmsPageBody Body)
    : IRequest<Result<CmsPageAdminDto>>;

public sealed class UpdateCmsPageCommandHandler(
    ICmsRepository cmsRepository,
    IContentSanitizer contentSanitizer,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UpdateCmsPageCommandHandler> logger)
    : IRequestHandler<UpdateCmsPageCommand, Result<CmsPageAdminDto>>
{
    public async Task<Result<CmsPageAdminDto>> Handle(
        UpdateCmsPageCommand request,
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

        var body = request.Body;
        if (CmsResults.HasRowVersionMismatch(body.RowVersion, page.RowVersion))
        {
            return CmsResults.ConcurrencyFailure<CmsPageAdminDto>();
        }

        if (!string.IsNullOrWhiteSpace(body.Slug))
        {
            var normalizedSlug = body.Slug.Trim().ToLowerInvariant();
            if (!string.Equals(page.Slug, normalizedSlug, StringComparison.Ordinal))
            {
                if (page.IsSystemPage)
                {
                    return Result<CmsPageAdminDto>.Failure(
                        ["System page slugs cannot be changed."],
                        [CmsErrorCodes.PageSystemSlugImmutable]);
                }

                if (!CmsSlugValidator.IsValidCustomSlug(normalizedSlug))
                {
                    var errorCode = CmsSlugs.IsReserved(normalizedSlug)
                        ? CmsErrorCodes.PageSlugReserved
                        : CmsErrorCodes.PageSlugInvalid;

                    return Result<CmsPageAdminDto>.Failure(
                        ["Invalid CMS page slug."],
                        [errorCode]);
                }

                if (await cmsRepository.IsPageSlugTakenAsync(normalizedSlug, page.Id, cancellationToken))
                {
                    return Result<CmsPageAdminDto>.Failure(
                        ["CMS page slug is already taken."],
                        [CmsErrorCodes.PageSlugDuplicate]);
                }

                page.RenameSlug(normalizedSlug);
            }
        }

        page.UpdateContent(
            body.TitleAr,
            body.TitleEn,
            contentSanitizer.SanitizeHtml(body.ContentAr),
            contentSanitizer.SanitizeHtml(body.ContentEn),
            body.MetaTitleAr,
            body.MetaTitleEn,
            body.MetaDescriptionAr,
            body.MetaDescriptionEn);

        var conflict = await CmsResults.TrySaveAsync<CmsPageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsPageUpdated,
            "CmsPage",
            page.Id.ToString(),
            $"Updated CMS page '{page.Slug}'.",
            cancellationToken);

        logger.LogInformation("Updated CMS page {PageId}.", page.Id);

        return Result<CmsPageAdminDto>.Success(CmsPageAdminDto.FromEntity(page));
    }
}
