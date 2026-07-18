using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Application.SupportTickets.Options;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.CreateSupportTicket;

public sealed record CreateSupportTicketCommand(CreateSupportTicketRequest Body)
    : IRequest<Result<SupportTicketParentDetailDto>>;

public sealed class CreateSupportTicketCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    ISupportTicketReferenceGenerator referenceGenerator,
    IAdmissionApplicationRepository admissionRepository,
    INotificationOutboxPublisher notificationPublisher,
    IParentAccountService parentAccountService,
    IOptions<SupportTicketSlaOptions> slaOptions,
    IUnitOfWork unitOfWork,
    ILogger<CreateSupportTicketCommandHandler> logger)
    : IRequestHandler<CreateSupportTicketCommand, Result<SupportTicketParentDetailDto>>
{
    public async Task<Result<SupportTicketParentDetailDto>> Handle(
        CreateSupportTicketCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsParent(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketParentDetailDto>();
        }

        var body = request.Body;
        if (!Enum.IsDefined(typeof(SupportTicketCategory), body.Category))
        {
            return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                "Invalid category.",
                SupportTicketErrorCodes.InvalidCategory);
        }

        if (!Enum.IsDefined(typeof(SupportTicketPriority), body.Priority))
        {
            return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                "Invalid priority.",
                SupportTicketErrorCodes.InvalidPriority);
        }

        Guid? admissionId = body.AdmissionApplicationId;
        if (admissionId is { } appId)
        {
            var admission = await admissionRepository.GetOwnedAsync(userId, appId, cancellationToken);
            if (admission is null)
            {
                return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                    "Admission application not found.",
                    SupportTicketErrorCodes.AdmissionNotFound);
            }
        }

        var createdAt = DateTimeOffset.UtcNow;
        var (firstDue, resolutionDue) = slaOptions.Value.ComputeDueDates(body.Priority, createdAt);
        var reference = await referenceGenerator.GenerateAsync(cancellationToken);

        var ticket = new SupportTicket(
            reference,
            userId,
            (SupportTicketCategory)body.Category,
            (SupportTicketPriority)body.Priority,
            body.Subject,
            admissionId,
            sourceContactRequestId: null,
            firstDue,
            resolutionDue);

        ticket.AddMessage(userId, SupportTicketAuthorType.Parent, SupportTicketMessageVisibility.CustomerVisible, body.Body);
        ticket.AddHistory(
            SupportTicketHistoryAction.Created,
            userId,
            fromValue: null,
            toValue: ticket.Status.ToString(),
            summary: "Ticket created");

        await ticketRepository.AddAsync(ticket, cancellationToken);

        await SupportTicketNotificationSupport.EnqueueAsync(
            notificationPublisher,
            parentAccountService,
            ticket,
            NotificationEventType.SupportTicketCreated,
            "created",
            cancellationToken);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketParentDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        logger.LogInformation("Parent {ParentUserId} created support ticket {Reference}.", userId, ticket.Reference);

        var reloaded = await ticketRepository.GetOwnedAsync(userId, ticket.Id, includeDetails: true, cancellationToken)
                       ?? ticket;
        return Result<SupportTicketParentDetailDto>.Success(SupportTicketMapping.ToParentDetail(reloaded));
    }
}
