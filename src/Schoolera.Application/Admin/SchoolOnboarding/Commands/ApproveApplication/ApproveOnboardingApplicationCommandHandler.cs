using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admin.SchoolOnboarding.Commands.ApproveApplication;

public sealed record ApproveOnboardingApplicationCommand(
    Guid ApplicationId,
    string? InternalNote) : IRequest<Result<AdminOnboardingDetailDto>>;

public sealed class ApproveOnboardingApplicationCommandHandler(
    ISchoolOnboardingRepository repository,
    ISchoolRepository schoolRepository,
    ISchoolReadRepository schoolReadRepository,
    IUserDirectory userDirectory,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IAdminPlatformService adminPlatform,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<ApproveOnboardingApplicationCommandHandler> logger)
    : IRequestHandler<ApproveOnboardingApplicationCommand, Result<AdminOnboardingDetailDto>>
{
    public async Task<Result<AdminOnboardingDetailDto>> Handle(
        ApproveOnboardingApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId;
        if (actorId is null)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "Forbidden", OnboardingErrorCodes.Forbidden);
        }

        var application = await repository.GetByIdAsync(request.ApplicationId, includeChildren: true, cancellationToken);
        if (application is null)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        // Idempotency: a previously approved application with a linked school returns the
        // existing state without creating any duplicate school, branch, or history records.
        if (application.Status == SchoolOnboardingStatus.Approved && application.ApprovedSchoolId is not null)
        {
            return Result<AdminOnboardingDetailDto>.Success(
                await OnboardingAdminReadModel.BuildAsync(application, repository, userDirectory, cancellationToken));
        }

        if (application.Status != SchoolOnboardingStatus.UnderReview)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "InvalidStatusTransition", OnboardingErrorCodes.InvalidStatusTransition);
        }

        var blockingCodes = await OnboardingSubmission.GetBlockingCodesAsync(application, repository, cancellationToken);
        if (blockingCodes.Count > 0)
        {
            return OnboardingResults.FailureForCodes<AdminOnboardingDetailDto>(localizer, blockingCodes);
        }

        var now = DateTimeOffset.UtcNow;
        var slug = await GenerateUniqueSlugAsync(application, cancellationToken);

        var school = new School(
            application.SchoolNameAr!,
            application.SchoolNameEn,
            slug,
            application.SchoolType!.Value,
            application.GenderType!.Value,
            SchoolStatus.Unpublished);
        school.AssignOwner(application.OwnerUserId);
        school.ApplyOnboardingProfile(
            application.SchoolShortDescriptionAr,
            application.SchoolShortDescriptionEn,
            application.FoundedYear,
            application.SchoolWebsiteUrl,
            application.PublicPhone,
            application.PublicEmail,
            application.WhatsAppOrAlternatePhone);

        var branch = new SchoolBranch(
            school.Id,
            application.SchoolNameAr!,
            application.SchoolNameEn,
            "main",
            application.CityId!.Value,
            application.DistrictId!.Value,
            isMainBranch: true);
        branch.UpdateAddress(
            application.AddressLineAr,
            application.AddressLineEn,
            application.BuildingNumber,
            application.StreetName,
            application.Landmark,
            application.PostalCode,
            application.LocalAddressReference,
            application.Latitude,
            application.Longitude);
        branch.UpdateContact(application.PublicPhone, application.PublicEmail);
        school.AddBranch(branch);

        await schoolRepository.AddAsync(school, cancellationToken);

        application.Approve(actorId.Value, school.Id, now);
        application.AddStatusHistory(new SchoolOnboardingStatusHistory(
            application.Id,
            SchoolOnboardingStatus.UnderReview,
            SchoolOnboardingStatus.Approved,
            actorId.Value,
            null,
            string.IsNullOrWhiteSpace(request.InternalNote) ? null : request.InternalNote.Trim()));

        var conflict = await OnboardingResults.TrySaveAsync<AdminOnboardingDetailDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Approved onboarding application {ApplicationId}; created school {SchoolId} (slug {Slug}).",
            application.Id,
            school.Id,
            slug);

        await adminPlatform.WriteAuditAsync(
            actorId.Value,
            AdminAuditActions.OnboardingApproved,
            "OnboardingApplication",
            application.Id.ToString(),
            $"Approved; school {school.Id}",
            cancellationToken);

        return Result<AdminOnboardingDetailDto>.Success(
            await OnboardingAdminReadModel.BuildAsync(application, repository, userDirectory, cancellationToken));
    }

    private async Task<string> GenerateUniqueSlugAsync(
        SchoolOnboardingApplication application,
        CancellationToken cancellationToken)
    {
        var source = !string.IsNullOrWhiteSpace(application.RequestedSlug)
            ? application.RequestedSlug!
            : !string.IsNullOrWhiteSpace(application.SchoolNameEn)
                ? application.SchoolNameEn!
                : application.SchoolNameAr!;

        var baseSlug = SlugHelper.Normalize(source);
        var candidate = baseSlug;
        var suffix = 2;

        while (await schoolReadRepository.SlugExistsAsync(candidate, null, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return candidate;
    }
}
