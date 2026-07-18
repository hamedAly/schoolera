using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Cms.Commands.SubmitContactRequest;

public sealed record SubmitContactRequestCommand(ContactRequestBody Body)
    : IRequest<Result<ContactRequestResultDto>>;

public sealed class SubmitContactRequestCommandHandler(
    ICmsRepository cmsRepository,
    IUnitOfWork unitOfWork,
    ILogger<SubmitContactRequestCommandHandler> logger)
    : IRequestHandler<SubmitContactRequestCommand, Result<ContactRequestResultDto>>
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(2);

    public async Task<Result<ContactRequestResultDto>> Handle(
        SubmitContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var body = request.Body;
        var category = ContactCategories.Normalize(body.Category)!;
        var source = ContactSources.Normalize(body.Source)!;
        var phone = body.Phone.Trim();
        var subject = body.Subject.Trim();

        if (await cmsRepository.HasRecentContactDuplicateAsync(
                body.Email,
                phone,
                subject,
                DuplicateWindow,
                cancellationToken))
        {
            logger.LogWarning("Rejected duplicate contact request within cooldown window.");
            return Result<ContactRequestResultDto>.Failure(
                ["Unable to submit the request."],
                [ContactErrorCodes.Rejected]);
        }

        var reference = await cmsRepository.GenerateContactReferenceAsync(cancellationToken);
        var contactRequest = new ContactRequest(
            reference,
            body.Name,
            phone,
            body.Email,
            category,
            subject,
            body.Message,
            source);

        await cmsRepository.AddContactRequestAsync(contactRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stored contact request {ContactRequestId} with reference {Reference}.",
            contactRequest.Id,
            contactRequest.Reference);

        return Result<ContactRequestResultDto>.Success(new ContactRequestResultDto(contactRequest.Reference));
    }
}
