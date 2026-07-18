using MediatR;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Legal.Dtos;

namespace Schoolera.Application.Legal.Queries.GetCurrentLegalDocuments;

public sealed record GetCurrentLegalDocumentsQuery : IRequest<Result<CurrentLegalDocumentsDto>>;

public sealed class GetCurrentLegalDocumentsQueryHandler(ILegalConsentService legalConsentService)
    : IRequestHandler<GetCurrentLegalDocumentsQuery, Result<CurrentLegalDocumentsDto>>
{
    public async Task<Result<CurrentLegalDocumentsDto>> Handle(
        GetCurrentLegalDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var ensure = await legalConsentService.EnsureCurrentVersionsExistAsync(cancellationToken);
        if (!ensure.Succeeded)
        {
            return Result<CurrentLegalDocumentsDto>.Failure(ensure.Errors, ensure.ErrorCodes);
        }

        var documents = await legalConsentService.GetCurrentDocumentsAsync(cancellationToken);
        return Result<CurrentLegalDocumentsDto>.Success(documents);
    }
}
