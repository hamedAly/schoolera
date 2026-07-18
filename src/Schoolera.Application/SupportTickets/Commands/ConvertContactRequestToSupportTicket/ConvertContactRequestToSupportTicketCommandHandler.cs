using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Options;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.ConvertContactRequestToSupportTicket;

public sealed record ConvertContactRequestToSupportTicketCommand(Guid ContactRequestId)
    : IRequest<Result<SupportTicketSupportDetailDto>>;

public sealed class ConvertContactRequestToSupportTicketCommandHandler(
    ICurrentUser currentUser,
    ICmsRepository cmsRepository,
    ISupportTicketRepository ticketRepository,
    ISupportTicketReferenceGenerator referenceGenerator,
    IUserDirectory userDirectory,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IOptions<SupportTicketSlaOptions> slaOptions,
    IUnitOfWork unitOfWork,
    ILogger<ConvertContactRequestToSupportTicketCommandHandler> logger)
    : IRequestHandler<ConvertContactRequestToSupportTicketCommand, Result<SupportTicketSupportDetailDto>>
{
    public async Task<Result<SupportTicketSupportDetailDto>> Handle(
        ConvertContactRequestToSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsPlatformAdmin(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketSupportDetailDto>();
        }

        var contactRequest = await cmsRepository.GetContactRequestByIdAsync(
            request.ContactRequestId,
            cancellationToken);
        if (contactRequest is null)
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Contact request not found.",
                SupportTicketErrorCodes.ContactNotFound);
        }

        var existing = await ticketRepository.GetBySourceContactRequestIdAsync(
            contactRequest.Id,
            cancellationToken);
        if (existing is not null)
        {
            return await BuildDetailAsync(existing.Id, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(contactRequest.Email))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Contact request email does not match a parent account.",
                SupportTicketErrorCodes.ContactEmailMismatch);
        }

        var parent = await userDirectory.FindByEmailAsync(contactRequest.Email, cancellationToken);
        if (parent is null)
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Contact request email does not match a parent account.",
                SupportTicketErrorCodes.ContactEmailMismatch);
        }

        var roles = await userDirectory.GetRolesAsync(parent.Id, cancellationToken);
        if (!roles.Contains(SchooleraRoles.Parent))
        {
            return SupportTicketResults.Failure<SupportTicketSupportDetailDto>(
                "Contact request email does not match a parent account.",
                SupportTicketErrorCodes.ContactEmailMismatch);
        }

        var createdAt = DateTimeOffset.UtcNow;
        var priority = SupportTicketPriority.Normal;
        var (firstDue, resolutionDue) = slaOptions.Value.ComputeDueDates((int)priority, createdAt);
        var reference = await referenceGenerator.GenerateAsync(cancellationToken);

        var ticket = new SupportTicket(
            reference,
            parent.Id,
            SupportTicketCategory.GeneralSupport,
            priority,
            contactRequest.Subject,
            admissionApplicationId: null,
            sourceContactRequestId: contactRequest.Id,
            firstDue,
            resolutionDue);

        ticket.AddMessage(
            parent.Id,
            SupportTicketAuthorType.Parent,
            SupportTicketMessageVisibility.CustomerVisible,
            contactRequest.Message);

        ticket.AddHistory(
            SupportTicketHistoryAction.ConvertedFromContactRequest,
            userId,
            fromValue: contactRequest.Reference,
            toValue: ticket.Reference,
            summary: "Converted from contact request");

        await ticketRepository.AddAsync(ticket, cancellationToken);

        await SupportTicketNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            ticket,
            NotificationEventType.SupportTicketCreated,
            "created",
            cancellationToken);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketSupportDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation(
            "Platform admin {UserId} converted contact request {ContactRequestId} to support ticket {Reference}.",
            userId,
            contactRequest.Id,
            ticket.Reference);

        return await BuildDetailAsync(ticket.Id, cancellationToken);
    }

    private async Task<Result<SupportTicketSupportDetailDto>> BuildDetailAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var reloaded = await ticketRepository.GetByIdAsync(ticketId, includeDetails: true, cancellationToken);
        if (reloaded is null)
        {
            return SupportTicketResults.NotFound<SupportTicketSupportDetailDto>();
        }

        var parents = await userDirectory.GetUsersAsync([reloaded.ParentUserId], cancellationToken);
        parents.TryGetValue(reloaded.ParentUserId, out var parent);
        return Result<SupportTicketSupportDetailDto>.Success(
            SupportTicketMapping.ToSupportDetail(reloaded, parent, utcNow));
    }
}
