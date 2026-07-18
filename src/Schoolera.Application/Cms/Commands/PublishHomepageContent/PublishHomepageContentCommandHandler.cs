using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Common;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Commands.PublishHomepageContent;

public sealed record PublishHomepageContentCommand(Guid Id) : IRequest<Result<HomepageAdminDto>>;

public sealed class PublishHomepageContentCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<PublishHomepageContentCommandHandler> logger)
    : IRequestHandler<PublishHomepageContentCommand, Result<HomepageAdminDto>>
{
    public async Task<Result<HomepageAdminDto>> Handle(
        PublishHomepageContentCommand request,
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

        if (!CmsUrlValidator.IsValidCtaUrl(content.PrimaryCtaUrl) ||
            (!string.IsNullOrWhiteSpace(content.SecondaryCtaUrl) &&
             !CmsUrlValidator.IsValidCtaUrl(content.SecondaryCtaUrl)))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Homepage CTA URLs are invalid."],
                [CmsErrorCodes.HomeInvalidCtaUrl]);
        }

        if (string.IsNullOrWhiteSpace(content.HeroTitleAr) ||
            string.IsNullOrWhiteSpace(content.HeroTitleEn) ||
            string.IsNullOrWhiteSpace(content.HeroSubtitleAr) ||
            string.IsNullOrWhiteSpace(content.HeroSubtitleEn))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Arabic and English hero content is required to publish."],
                [CmsErrorCodes.PagePublishValidation]);
        }

        content.Publish();

        var conflict = await CmsResults.TrySaveAsync<HomepageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsHomePublished,
            "HomepageContent",
            content.Id.ToString(),
            "Published homepage content.",
            cancellationToken);

        logger.LogInformation("Published homepage content {ContentId}.", content.Id);

        return Result<HomepageAdminDto>.Success(HomepageAdminDto.FromEntity(content));
    }
}
