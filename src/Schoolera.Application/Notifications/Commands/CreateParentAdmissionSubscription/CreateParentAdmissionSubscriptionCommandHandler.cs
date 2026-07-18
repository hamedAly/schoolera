using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Notifications.Common;
using Schoolera.Application.Notifications.Dtos;
using Schoolera.Application.Parent.Constants;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Notifications.Commands.CreateParentAdmissionSubscription;

public sealed record CreateParentAdmissionSubscriptionCommand(
    CreateParentAdmissionOpenSubscriptionRequest Body)
    : IRequest<Result<ParentAdmissionOpenSubscriptionDto>>;

public sealed class CreateParentAdmissionSubscriptionCommandHandler(
    ICurrentUser currentUser,
    INotificationRepository notificationRepository,
    ISchoolPortalRepository schoolPortalRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateParentAdmissionSubscriptionCommandHandler> logger)
    : IRequestHandler<CreateParentAdmissionSubscriptionCommand, Result<ParentAdmissionOpenSubscriptionDto>>
{
    public async Task<Result<ParentAdmissionOpenSubscriptionDto>> Handle(
        CreateParentAdmissionSubscriptionCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ParentAdmissionOpenSubscriptionDto>.Failure(
                ["Forbidden."],
                [ParentErrorCodes.Forbidden]);
        }

        var body = request.Body;
        if (!Enum.IsDefined(body.PreferredChannel))
        {
            return Result<ParentAdmissionOpenSubscriptionDto>.Failure(
                ["Invalid preferred channel."],
                [ParentErrorCodes.Forbidden]);
        }

        var school = await schoolPortalRepository.GetSchoolProfileAsync(
            body.SchoolId,
            cancellationToken);
        if (school is null)
        {
            return Result<ParentAdmissionOpenSubscriptionDto>.Failure(
                ["School not found."],
                [ParentErrorCodes.SchoolNotFound]);
        }

        var existing = await notificationRepository.FindActiveSubscriptionAsync(
            userId,
            body.SchoolId,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            cancellationToken);
        if (existing is not null)
        {
            return Result<ParentAdmissionOpenSubscriptionDto>.Failure(
                ["An active subscription already exists for this selection."],
                [ParentErrorCodes.DuplicateSubscription]);
        }

        var subscription = new ParentAdmissionOpenSubscription(
            userId,
            body.SchoolId,
            body.SchoolBranchId,
            body.EducationalStageId,
            body.GradeId,
            body.AcademicYearId,
            body.PreferredChannel);

        await notificationRepository.AddSubscriptionAsync(subscription, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created admission-open subscription {SubscriptionId} for parent {ParentUserId} school {SchoolId}.",
            subscription.Id,
            userId,
            body.SchoolId);

        return Result<ParentAdmissionOpenSubscriptionDto>.Success(
            ParentNotificationMapping.ToSubscriptionDto(subscription));
    }
}
