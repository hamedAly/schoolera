using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.RequestMissingDocuments;

public sealed record RequestMissingDocumentsCommand(
    Guid SchoolId,
    Guid ApplicationId,
    RequestMissingDocumentsRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class RequestMissingDocumentsCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    INotificationOutboxPublisher notificationOutboxPublisher,
    IParentAccountService parentAccountService,
    ISchoolPortalRepository schoolPortalRepository,
    ILogger<RequestMissingDocumentsCommandHandler> logger)
    : IRequestHandler<RequestMissingDocumentsCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        RequestMissingDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolAdmissionApplicationDetailDto>.Failure(
                accessResult.Errors,
                accessResult.ErrorCodes);
        }

        var access = accessResult.Data;
        var application = await admissionRepository.GetForSchoolForUpdateAsync(
            request.SchoolId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.ReviewNotFound<SchoolAdmissionApplicationDetailDto>();
        }

        if (AdmissionResults.HasRowVersionMismatch(request.Body.RowVersion, application.RowVersion))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "The application was modified by another operation.",
                AdmissionErrorCodes.ReviewConcurrentUpdate);
        }

        if (!AdmissionTransitionPolicy.TryValidateSchoolTransition(
                application.Status,
                AdmissionApplicationStatus.MissingDocuments,
                out var transitionCode))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Invalid status transition.",
                transitionCode);
        }

        if (application.ActiveMissingItemsRequest is not null)
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "An active missing-items request already exists.",
                AdmissionErrorCodes.ReviewMissingItemsRequired);
        }

        var refs = request.Body.Items ?? [];
        if (refs.Count == 0 || string.IsNullOrWhiteSpace(request.Body.ParentVisibleReason))
        {
            return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                "Missing items and a parent-visible reason are required.",
                AdmissionErrorCodes.ReviewMissingItemsRequired);
        }

        var missingRequest = new AdmissionMissingItemsRequest(
            application.Id,
            request.Body.ParentVisibleReason,
            request.Body.Instructions,
            request.Body.ResponseDeadlineUtc,
            access.UserId);

        foreach (var itemRef in refs)
        {
            var built = TryBuildItem(application, missingRequest.Id, itemRef, out var errorCode);
            if (built is null)
            {
                return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
                    "One or more missing items are invalid for this application.",
                    errorCode ?? AdmissionErrorCodes.MissingItemsInvalid);
            }

            missingRequest.AddItem(built);
        }

        var fromStatus = application.Status;
        var actorRole = access.HistoryActorRole;
        var internalNote = string.IsNullOrWhiteSpace(request.Body.InternalReviewNote)
            ? null
            : request.Body.InternalReviewNote.Trim();

        application.MoveToMissingDocuments();
        application.AddMissingItemsRequest(missingRequest);
        admissionRepository.AddHistory(
            new AdmissionApplicationHistory(
                application.Id,
                fromStatus,
                AdmissionApplicationStatus.MissingDocuments,
                AdmissionHistoryActions.MissingDocumentsRequested,
                access.UserId,
                actorRole,
                parentVisible: true,
                parentVisibleNote: request.Body.ParentVisibleReason.Trim(),
                internalNote: internalNote));

        await AdmissionParentNotificationSupport.EnqueueAsync(
            notificationOutboxPublisher,
            parentAccountService,
            schoolPortalRepository,
            application,
            NotificationEventType.MissingInformationRequested,
            "missing-documents-requested",
            cancellationToken);

        var conflict = await AdmissionResults.TrySaveAsync<SchoolAdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return AdmissionResults.RemapSchoolSaveConflict(conflict);
        }

        var loaded = await admissionRepository.GetForSchoolAsync(
            request.SchoolId,
            application.Id,
            cancellationToken) ?? application;
        var users = await userDirectory.GetUsersAsync([loaded.ParentUserId], cancellationToken);
        users.TryGetValue(loaded.ParentUserId, out var parentUser);

        logger.LogInformation(
            "Requested missing documents for application {ApplicationId} ({ItemCount} items).",
            loaded.Id,
            missingRequest.Items.Count);

        return Result<SchoolAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToSchoolDetail(
                loaded,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector));
    }

    private static AdmissionMissingItem? TryBuildItem(
        AdmissionApplication application,
        Guid requestId,
        AdmissionMissingItemRefDto itemRef,
        out string? errorCode)
    {
        errorCode = AdmissionErrorCodes.MissingItemsInvalid;

        return itemRef.Kind switch
        {
            AdmissionMissingItemKind.RequirementSnapshot => BuildRequirement(application, requestId, itemRef, ref errorCode),
            AdmissionMissingItemKind.QuestionSnapshot => BuildQuestion(application, requestId, itemRef, ref errorCode),
            AdmissionMissingItemKind.ParentSnapshotField => BuildParentField(requestId, itemRef, ref errorCode),
            AdmissionMissingItemKind.ChildSnapshotField => BuildChildField(requestId, itemRef, ref errorCode),
            _ => null,
        };
    }

    private static AdmissionMissingItem? BuildRequirement(
        AdmissionApplication application,
        Guid requestId,
        AdmissionMissingItemRefDto itemRef,
        ref string? errorCode)
    {
        if (itemRef.RequirementSnapshotId is null)
        {
            return null;
        }

        var snapshot = application.RequirementSnapshots?
            .FirstOrDefault(item => item.Id == itemRef.RequirementSnapshotId.Value);
        if (snapshot is null)
        {
            return null;
        }

        return new AdmissionMissingItem(
            requestId,
            AdmissionMissingItemKind.RequirementSnapshot,
            itemRef.IsMandatory,
            snapshot.NameAr,
            snapshot.NameEn,
            requirementSnapshotId: snapshot.Id);
    }

    private static AdmissionMissingItem? BuildQuestion(
        AdmissionApplication application,
        Guid requestId,
        AdmissionMissingItemRefDto itemRef,
        ref string? errorCode)
    {
        if (itemRef.QuestionSnapshotId is null)
        {
            return null;
        }

        var snapshot = application.QuestionSnapshots?
            .FirstOrDefault(item => item.Id == itemRef.QuestionSnapshotId.Value);
        if (snapshot is null)
        {
            return null;
        }

        return new AdmissionMissingItem(
            requestId,
            AdmissionMissingItemKind.QuestionSnapshot,
            itemRef.IsMandatory,
            snapshot.LabelAr,
            snapshot.LabelEn,
            questionSnapshotId: snapshot.Id);
    }

    private static AdmissionMissingItem? BuildParentField(
        Guid requestId,
        AdmissionMissingItemRefDto itemRef,
        ref string? errorCode)
    {
        if (itemRef.ParentSnapshotField is null ||
            !Enum.IsDefined(itemRef.ParentSnapshotField.Value))
        {
            return null;
        }

        var code = itemRef.ParentSnapshotField.Value;
        var label = code.ToString();
        return new AdmissionMissingItem(
            requestId,
            AdmissionMissingItemKind.ParentSnapshotField,
            itemRef.IsMandatory,
            labelAr: label,
            labelEn: label,
            parentSnapshotField: code);
    }

    private static AdmissionMissingItem? BuildChildField(
        Guid requestId,
        AdmissionMissingItemRefDto itemRef,
        ref string? errorCode)
    {
        if (itemRef.ChildSnapshotField is null ||
            !Enum.IsDefined(itemRef.ChildSnapshotField.Value))
        {
            return null;
        }

        var code = itemRef.ChildSnapshotField.Value;
        var label = code.ToString();
        return new AdmissionMissingItem(
            requestId,
            AdmissionMissingItemKind.ChildSnapshotField,
            itemRef.IsMandatory,
            labelAr: label,
            labelEn: label,
            childSnapshotField: code);
    }
}
