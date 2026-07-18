using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.SupportTickets.Common;
using Schoolera.Application.SupportTickets.Constants;
using Schoolera.Application.SupportTickets.Dtos;

namespace Schoolera.Application.SupportTickets.Queries.DownloadSupportTicketAttachment;

public sealed record DownloadSupportTicketAttachmentQuery(Guid TicketId, Guid AttachmentId)
    : IRequest<Result<SupportTicketAttachmentDownloadDto>>;

public sealed class DownloadSupportTicketAttachmentQueryValidator
    : AbstractValidator<DownloadSupportTicketAttachmentQuery>
{
    public DownloadSupportTicketAttachmentQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
        RuleFor(query => query.AttachmentId).NotEmpty();
    }
}

public sealed class DownloadSupportTicketAttachmentQueryHandler(
    ICurrentUser currentUser,
    ISupportTicketRepository ticketRepository,
    IPrivateFileStorage privateFileStorage,
    ILogger<DownloadSupportTicketAttachmentQueryHandler> logger)
    : IRequestHandler<DownloadSupportTicketAttachmentQuery, Result<SupportTicketAttachmentDownloadDto>>
{
    public async Task<Result<SupportTicketAttachmentDownloadDto>> Handle(
        DownloadSupportTicketAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        if (!SupportTicketAccess.IsSupportOrAdmin(currentUser))
        {
            return SupportTicketResults.Forbidden<SupportTicketAttachmentDownloadDto>();
        }

        var ticket = await ticketRepository.GetByIdAsync(
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
        if (attachment is null)
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
