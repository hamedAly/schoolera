using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Commands.UploadSupportTicketAttachmentAsParent;

public sealed record UploadSupportTicketAttachmentAsParentCommand(
    Guid TicketId,
    Stream Content,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    Guid? MessageId = null) : IRequest<Result<SupportTicketParentDetailDto>>
{
    public static UploadSupportTicketAttachmentAsParentCommand FromRaw(
        Guid ticketId,
        Stream content,
        string originalFileName,
        string contentType,
        long fileSize,
        Guid? messageId = null) =>
        new(ticketId, content, originalFileName, contentType, fileSize, messageId);
}

public sealed class UploadSupportTicketAttachmentAsParentCommandHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    ILogger<UploadSupportTicketAttachmentAsParentCommandHandler> logger)
    : IRequestHandler<UploadSupportTicketAttachmentAsParentCommand, Result<SupportTicketParentDetailDto>>
{
    public async Task<Result<SupportTicketParentDetailDto>> Handle(
        UploadSupportTicketAttachmentAsParentCommand request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsParent(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketParentDetailDto>();
        }

        var ticket = await ticketRepository.GetOwnedAsync(
            userId,
            request.TicketId,
            includeDetails: true,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<SupportTicketParentDetailDto>();
        }

        if (ticket.Status == SupportTicketStatus.Closed)
        {
            return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                "Attachments cannot be uploaded for the current ticket status.",
                SupportTicketErrorCodes.InvalidStatus);
        }

        if (request.MessageId is { } messageId)
        {
            var message = ticket.Messages.FirstOrDefault(entry => entry.Id == messageId);
            if (message is null ||
                message.Visibility != SupportTicketMessageVisibility.CustomerVisible)
            {
                return SupportTicketResults.NotFound<SupportTicketParentDetailDto>();
            }
        }

        StoredPrivateFile stored;
        try
        {
            stored = await privateFileStorage.SaveAsync(
                new PrivateFileStoreRequest
                {
                    Content = request.Content,
                    OriginalFileName = request.OriginalFileName,
                    ContentType = request.ContentType,
                    DeclaredSizeBytes = request.FileSize,
                    Category = $"support-tickets/{ticket.Id:N}",
                },
                cancellationToken);
        }
        catch (PrivateFileValidationException exception)
        {
            logger.LogWarning("Support ticket attachment upload rejected: {Code}.", exception.ErrorCode);
            return SupportTicketResults.Failure<SupportTicketParentDetailDto>(
                "Attachment is invalid.",
                SupportTicketErrorCodes.AttachmentInvalid);
        }

        var safeName = SupportTicketMapping.SafeOriginalFileName(request.OriginalFileName);
        ticket.AddAttachment(
            request.MessageId,
            userId,
            SupportTicketMessageVisibility.CustomerVisible,
            stored.StoredFileReference,
            safeName,
            stored.ContentType,
            stored.SizeBytes);

        ticket.AddHistory(
            SupportTicketHistoryAction.AttachmentAdded,
            userId,
            fromValue: null,
            toValue: safeName,
            summary: null);

        var conflict = await SupportTicketResults.TrySaveAsync<SupportTicketParentDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            await privateFileStorage.DeleteAsync(stored.StoredFileReference, cancellationToken);
            return conflict;
        }

        logger.LogInformation(
            "Parent {UserId} uploaded attachment to support ticket {TicketId}.",
            userId,
            ticket.Id);

        var reloaded = await ticketRepository.GetOwnedAsync(
            userId,
            ticket.Id,
            includeDetails: true,
            cancellationToken) ?? ticket;
        return Result<SupportTicketParentDetailDto>.Success(SupportTicketMapping.ToParentDetail(reloaded));
    }
}
