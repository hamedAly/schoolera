using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Common;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Parent.Queries.ListChildDocuments;

public sealed record ListChildDocumentsQuery(Guid ChildId)
    : IRequest<Result<IReadOnlyList<ChildDocumentDto>>>;

public sealed class ListChildDocumentsQueryValidator : AbstractValidator<ListChildDocumentsQuery>
{
    public ListChildDocumentsQueryValidator()
    {
        RuleFor(query => query.ChildId).NotEmpty();
    }
}

public sealed class ListChildDocumentsQueryHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    IChildDocumentRepository childDocumentRepository,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<ListChildDocumentsQueryHandler> logger)
    : IRequestHandler<ListChildDocumentsQuery, Result<IReadOnlyList<ChildDocumentDto>>>
{
    public async Task<Result<IReadOnlyList<ChildDocumentDto>>> Handle(
        ListChildDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<IReadOnlyList<ChildDocumentDto>>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var child = await childProfileRepository.GetOwnedAsync(userId, request.ChildId, cancellationToken);
        if (child is null || !child.IsActive)
        {
            return Result<IReadOnlyList<ChildDocumentDto>>.Failure(
                ["Child not found."],
                [ParentErrorCodes.ChildNotFound]);
        }

        var documents = await childDocumentRepository.ListByChildAsync(
            userId,
            request.ChildId,
            cancellationToken);
        logger.LogInformation(
            "Listed {Count} vault documents for child {ChildId}.",
            documents.Count,
            request.ChildId);

        return Result<IReadOnlyList<ChildDocumentDto>>.Success(
            documents.Select(ChildDocumentMapping.ToDto).ToArray());
    }
}
