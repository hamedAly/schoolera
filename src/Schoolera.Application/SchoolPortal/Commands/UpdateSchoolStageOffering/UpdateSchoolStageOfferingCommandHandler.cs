using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolStageOffering;

public sealed record UpdateSchoolStageOfferingCommand(
    Guid SchoolId,
    Guid OfferingId,
    UpdateSchoolStageOfferingRequest Body) : IRequest<Result<SchoolStageOfferingDto>>;

public sealed class UpdateSchoolStageOfferingCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    INotificationRepository notificationRepository,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolStageOfferingCommandHandler> logger)
    : IRequestHandler<UpdateSchoolStageOfferingCommand, Result<SchoolStageOfferingDto>>
{
    public async Task<Result<SchoolStageOfferingDto>> Handle(
        UpdateSchoolStageOfferingCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolStageOfferingDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolStageOfferingDto>(
            accessResult.Data, SchoolPortalPermission.ManageOfferings, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var offering = await repository.GetOfferingForWriteAsync(
            request.SchoolId, request.OfferingId, cancellationToken);
        if (offering is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.OfferingNotFound);
        }

        var body = request.Body;
        var desiredGradeIds = body.GradeIds.Distinct().ToArray();
        var previousIsAdmissionOpen = offering.IsAdmissionOpen;

        if (!await repository.GradesBelongToStageAsync(
                offering.EducationalStageId, desiredGradeIds, cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.InvalidGrade);
        }

        if (body.GenderType != offering.GenderType &&
            await repository.DuplicateOfferingExistsAsync(
                offering.SchoolBranchId,
                offering.EducationalStageId,
                body.GenderType,
                offering.Id,
                cancellationToken))
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.DuplicateOffering);
        }

        offering.Update(body.GenderType, body.Capacity, body.IsAdmissionOpen);
        SyncGradeOfferings(offering, desiredGradeIds);

        if (!previousIsAdmissionOpen && body.IsAdmissionOpen)
        {
            var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);
            var schoolName = school?.NameAr
                ?? school?.NameEn
                ?? request.SchoolId.ToString();
            await EnqueueAdmissionsOpenedAsync(
                request, offering, desiredGradeIds, schoolName, cancellationToken);
        }

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolStageOfferingDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await repository.GetOfferingForWriteAsync(
            request.SchoolId, offering.Id, cancellationToken);
        if (loaded is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolStageOfferingDto>(
                localizer, SchoolPortalErrorCodes.OfferingNotFound);
        }

        return Result<SchoolStageOfferingDto>.Success(SchoolPortalReadModel.ToOffering(loaded));
    }

    private async Task EnqueueAdmissionsOpenedAsync(
        UpdateSchoolStageOfferingCommand request,
        SchoolStageOffering offering,
        IReadOnlyList<Guid> gradeIds,
        string schoolName,
        CancellationToken cancellationToken)
    {
        var subscriptions = await notificationRepository.FindMatchingActiveSubscriptionsAsync(
            request.SchoolId,
            offering.SchoolBranchId,
            offering.EducationalStageId,
            gradeIds,
            cancellationToken);

        if (subscriptions.Count == 0)
        {
            return;
        }

        var stageName = offering.EducationalStage?.NameAr
            ?? offering.EducationalStage?.NameEn
            ?? offering.EducationalStageId.ToString();

        foreach (var subscription in subscriptions)
        {
            var channels = new List<NotificationChannel> { NotificationChannel.InApp };
            if (subscription.PreferredChannel != NotificationChannel.InApp)
            {
                channels.Add(subscription.PreferredChannel);
            }

            await notificationOutboxPublisher.EnqueueAsync(
                new NotificationEnqueueRequest(
                    subscription.ParentUserId,
                    NotificationEventType.AdmissionsOpened,
                    Culture: "ar",
                    DeduplicationKeyBase: $"admissions-opened:{offering.Id}:{subscription.Id}",
                    Variables: new Dictionary<string, string>
                    {
                        ["schoolName"] = schoolName,
                        ["stageName"] = stageName,
                    },
                    ActionPath: $"/parent/schools/{request.SchoolId}",
                    RelatedSchoolId: request.SchoolId,
                    RelatedEntityId: offering.Id,
                    ForceChannels: channels),
                cancellationToken);
        }

        logger.LogInformation(
            "Queued admissions-opened notifications for offering {OfferingId} to {SubscriptionCount} subscription(s).",
            offering.Id,
            subscriptions.Count);
    }

    private static void SyncGradeOfferings(SchoolStageOffering offering, IReadOnlyList<Guid> desiredGradeIds)
    {
        var desiredSet = desiredGradeIds.ToHashSet();
        var existingByGradeId = offering.GradeOfferings.ToDictionary(grade => grade.GradeId);

        foreach (var gradeOffering in offering.GradeOfferings)
        {
            if (desiredSet.Contains(gradeOffering.GradeId))
            {
                gradeOffering.Activate();
            }
            else
            {
                gradeOffering.Deactivate();
            }
        }

        foreach (var gradeId in desiredGradeIds.Where(id => !existingByGradeId.ContainsKey(id)))
        {
            offering.AddGradeOffering(new SchoolGradeOffering(offering.Id, gradeId));
        }
    }
}
