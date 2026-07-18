using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Admin.Constants;
using Schoolera.Application.Admin.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admin.Queries.GetSchoolDetail;

public sealed record GetAdminSchoolDetailQuery(Guid SchoolId)
    : IRequest<Result<AdminSchoolDetailDto>>;

public sealed class GetAdminSchoolDetailQueryHandler(
    IAdminPlatformService adminPlatform,
    IStringLocalizer<AuthMessages> localizer)
    : IRequestHandler<GetAdminSchoolDetailQuery, Result<AdminSchoolDetailDto>>
{
    public async Task<Result<AdminSchoolDetailDto>> Handle(
        GetAdminSchoolDetailQuery request,
        CancellationToken cancellationToken)
    {
        var school = await adminPlatform.GetSchoolAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return Result<AdminSchoolDetailDto>.Failure(
                [localizer["AdminSchoolNotFound"].Value],
                [AdminErrorCodes.SchoolNotFound]);
        }

        return Result<AdminSchoolDetailDto>.Success(school);
    }
}
