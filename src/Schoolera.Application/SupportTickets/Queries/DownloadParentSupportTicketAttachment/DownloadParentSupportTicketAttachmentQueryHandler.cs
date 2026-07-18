using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.SupportTickets.Queries.DownloadParentSupportTicketAttachment;

public sealed record DownloadParentSupportTicketAttachmentQuery(Guid TicketId, Guid AttachmentId)
    : IRequest<Result<SupportTicketAttachmentDownloadDto>>;

public sealed class DownloadParentSupportTicketAttachmentQueryValidator
    : AbstractValidator<DownloadParentSupportTicketAttachmentQuery>
{
    public DownloadParentSupportTicketAttachmentQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
        RuleFor(query => query.AttachmentId).NotEmpty();
    }
}

public sealed class DownloadParentSupportTicketAttachmentQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IPrivateFileStorage privateFileStorage,
    ILogger<DownloadParentSupportTicketAttachmentQueryHandler> logger)
    : IRequestHandler<DownloadParentSupportTicketAttachmentQuery, Result<SupportTicketAttachmentDownloadDto>>
{
    public async Task<Result<SupportTicketAttachmentDownloadDto>> Handle(
        DownloadParentSupportTicketAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsParent(currentUser) || currentUser.UserId is not { } userId)
        {
            return SupportTicketResults.Forbidden<SupportTicketAttachmentDownloadDto>();
        }

        var ticket = await ticketRepository.GetOwnedAsync(
            userId,
            request.TicketId,
            includeDetails: false,
            cancellationToken);
        if (ticket is null)
        {
            return SupportTicketResults.NotFound<SupportTicketAttachmentDownloadDto>();
        }

        var attachment = await ticketRepository.GetAttachmentAsync(
            request.TicketId,
            request.AttachmentId,
            cancellationToken);
        if (attachment is null ||
            attachment.Visibility != SupportTicketMessageVisibility.CustomerVisible)
        {
            return SupportTicketResults.Failure<SupportTicketAttachmentDownloadDto>(
                "Attachment not found.",
                SupportTicketErrorCodes.AttachmentNotFound);
        }

        var stream = await privateFileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (stream is null)
        {
            logger.LogWarning(
                "Stored file missing for support ticket attachment {AttachmentId}.",
                attachment.Id);
            return SupportTicketResults.Failure<SupportTicketAttachmentDownloadDto>(
                "Attachment not found.",
                SupportTicketErrorCodes.AttachmentNotFound);
        }

        return Result<SupportTicketAttachmentDownloadDto>.Success(
            new SupportTicketAttachmentDownloadDto(
                stream,
                attachment.ContentType,
                SupportTicketMapping.SafeOriginalFileName(attachment.OriginalFileName),
                attachment.SizeBytes));
    }
}
