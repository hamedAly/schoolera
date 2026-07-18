using MediatR;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Cms.Constants;
using Schoolera.Application.Cms.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Cms.Queries.GetContactRequestById;

public sealed record GetContactRequestByIdQuery(Guid Id) : IRequest<Result<ContactRequestDetailDto>>;

public sealed class GetContactRequestByIdQueryHandler(
    ICmsRepository cmsRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetContactRequestByIdQuery, Result<ContactRequestDetailDto>>
{
    public async Task<Result<ContactRequestDetailDto>> Handle(
        GetContactRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null || !currentUser.IsInRole(SchooleraRoles.PlatformAdmin))
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Forbidden."],
                [CmsErrorCodes.Forbidden]);
        }

        var contactRequest = await cmsRepository.GetContactRequestByIdAsync(request.Id, cancellationToken);
        if (contactRequest is null)
        {
            return Result<ContactRequestDetailDto>.Failure(
                ["Contact request not found."],
                [ContactErrorCodes.NotFound]);
        }

        return Result<ContactRequestDetailDto>.Success(CmsDtoMapping.ToDetail(contactRequest));
    }
}
