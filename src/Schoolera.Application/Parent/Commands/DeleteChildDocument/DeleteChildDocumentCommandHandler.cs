using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Parent.Commands.DeleteChildDocument;

public sealed record DeleteChildDocumentCommand(Guid ChildId, Guid DocumentId)
    : IRequest<Result<bool>>;

public sealed class DeleteChildDocumentCommandHandler(
    ICurrentUser currentUser,
    IChildDocumentRepository childDocumentRepository,
    IPrivateFileStorage privateFileStorage,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<DeleteChildDocumentCommandHandler> logger)
    : IRequestHandler<DeleteChildDocumentCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteChildDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<bool>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var document = await childDocumentRepository.GetOwnedForUpdateAsync(
            userId,
            request.ChildId,
            request.DocumentId,
            cancellationToken);
        if (document is null)
        {
            return Result<bool>.Failure(
                ["Document not found."],
                [ParentErrorCodes.DocumentNotFound]);
        }

        var storageKey = document.StorageKey;
        childDocumentRepository.Remove(document);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await privateFileStorage.DeleteAsync(storageKey, cancellationToken);

        logger.LogInformation(
            "Deleted child document {DocumentId} for child {ChildId}.",
            request.DocumentId,
            request.ChildId);

        return Result<bool>.Success(true);
    }
}
