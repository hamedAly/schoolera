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

namespace Schoolera.Application.Cms.Commands.UpdateHomepageContent;

public sealed record UpdateHomepageContentBody(
    string HeroTitleAr,
    string HeroTitleEn,
    string HeroSubtitleAr,
    string HeroSubtitleEn,
    string PrimaryCtaLabelAr,
    string PrimaryCtaLabelEn,
    string PrimaryCtaUrl,
    string? SecondaryCtaLabelAr,
    string? SecondaryCtaLabelEn,
    string? SecondaryCtaUrl,
    string SchoolsSectionTitleAr,
    string SchoolsSectionTitleEn,
    string ParentJourneyTitleAr,
    string ParentJourneyTitleEn,
    string ParentJourneyTextAr,
    string ParentJourneyTextEn,
    string SchoolJourneyTitleAr,
    string SchoolJourneyTitleEn,
    string SchoolJourneyTextAr,
    string SchoolJourneyTextEn,
    string FaqSectionTitleAr,
    string FaqSectionTitleEn,
    string? FaqSectionSubtitleAr,
    string? FaqSectionSubtitleEn,
    byte[]? RowVersion);

public sealed record UpdateHomepageContentCommand(UpdateHomepageContentBody Body)
    : IRequest<Result<HomepageAdminDto>>;

public sealed class UpdateHomepageContentCommandHandler(
    ICmsRepository cmsRepository,
    IAdminPlatformService adminPlatform,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UpdateHomepageContentCommandHandler> logger)
    : IRequestHandler<UpdateHomepageContentCommand, Result<HomepageAdminDto>>
{
    public async Task<Result<HomepageAdminDto>> Handle(
        UpdateHomepageContentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var body = request.Body;

        if (!CmsUrlValidator.IsValidCtaUrl(body.PrimaryCtaUrl))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Primary CTA URL is invalid."],
                [CmsErrorCodes.HomeInvalidCtaUrl]);
        }

        if (!string.IsNullOrWhiteSpace(body.SecondaryCtaUrl) &&
            !CmsUrlValidator.IsValidCtaUrl(body.SecondaryCtaUrl))
        {
            return Result<HomepageAdminDto>.Failure(
                ["Secondary CTA URL is invalid."],
                [CmsErrorCodes.HomeInvalidCtaUrl]);
        }

        var content = await cmsRepository.GetLatestHomepageDraftOrSingleAsync(cancellationToken);
        if (content is null)
        {
            content = new HomepageContent(
                body.HeroTitleAr,
                body.HeroTitleEn,
                body.HeroSubtitleAr,
                body.HeroSubtitleEn,
                body.PrimaryCtaLabelAr,
                body.PrimaryCtaLabelEn,
                body.PrimaryCtaUrl,
                body.SecondaryCtaLabelAr,
                body.SecondaryCtaLabelEn,
                body.SecondaryCtaUrl,
                body.SchoolsSectionTitleAr,
                body.SchoolsSectionTitleEn,
                body.ParentJourneyTitleAr,
                body.ParentJourneyTitleEn,
                body.ParentJourneyTextAr,
                body.ParentJourneyTextEn,
                body.SchoolJourneyTitleAr,
                body.SchoolJourneyTitleEn,
                body.SchoolJourneyTextAr,
                body.SchoolJourneyTextEn,
                body.FaqSectionTitleAr,
                body.FaqSectionTitleEn,
                body.FaqSectionSubtitleAr,
                body.FaqSectionSubtitleEn);

            await cmsRepository.AddHomepageAsync(content, cancellationToken);
        }
        else
        {
            if (body.RowVersion is { Length: > 0 } &&
                CmsResults.HasRowVersionMismatch(body.RowVersion, content.RowVersion))
            {
                return CmsResults.ConcurrencyFailure<HomepageAdminDto>();
            }

            content.Update(
                body.HeroTitleAr,
                body.HeroTitleEn,
                body.HeroSubtitleAr,
                body.HeroSubtitleEn,
                body.PrimaryCtaLabelAr,
                body.PrimaryCtaLabelEn,
                body.PrimaryCtaUrl,
                body.SecondaryCtaLabelAr,
                body.SecondaryCtaLabelEn,
                body.SecondaryCtaUrl,
                body.SchoolsSectionTitleAr,
                body.SchoolsSectionTitleEn,
                body.ParentJourneyTitleAr,
                body.ParentJourneyTitleEn,
                body.ParentJourneyTextAr,
                body.ParentJourneyTextEn,
                body.SchoolJourneyTitleAr,
                body.SchoolJourneyTitleEn,
                body.SchoolJourneyTextAr,
                body.SchoolJourneyTextEn,
                body.FaqSectionTitleAr,
                body.FaqSectionTitleEn,
                body.FaqSectionSubtitleAr,
                body.FaqSectionSubtitleEn);
        }

        var conflict = await CmsResults.TrySaveAsync<HomepageAdminDto>(unitOfWork, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await adminPlatform.WriteAuditAsync(
            actorId,
            AdminAuditActions.CmsHomeUpdated,
            "HomepageContent",
            content.Id.ToString(),
            "Updated homepage content.",
            cancellationToken);

        logger.LogInformation("Updated homepage content {ContentId}.", content.Id);

        return Result<HomepageAdminDto>.Success(HomepageAdminDto.FromEntity(content));
    }
}
