using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admissions.Queries.GetAdminAdmissionApplication;

public sealed record GetAdminAdmissionApplicationQuery(Guid ApplicationId)
    : IRequest<Result<AdminAdmissionApplicationDetailDto>>;

public sealed class GetAdminAdmissionApplicationQueryHandler(
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IChildIdentityProtector identityProtector,
    ILogger<GetAdminAdmissionApplicationQueryHandler> logger)
    : IRequestHandler<GetAdminAdmissionApplicationQuery, Result<AdminAdmissionApplicationDetailDto>>
{
    public async Task<Result<AdminAdmissionApplicationDetailDto>> Handle(
        GetAdminAdmissionApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var application = await admissionRepository.GetForAdminAsync(
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.ReviewNotFound<AdminAdmissionApplicationDetailDto>();
        }

        var users = await userDirectory.GetUsersAsync([application.ParentUserId], cancellationToken);
        users.TryGetValue(application.ParentUserId, out var parentUser);

        logger.LogInformation(
            "Loaded admin admission application {ApplicationId}.",
            application.Id);

        return Result<AdminAdmissionApplicationDetailDto>.Success(
            SchoolAdmissionMapping.ToAdminDetail(
                application,
                SchoolAdmissionMapping.ToParentContact(parentUser),
                identityProtector));
    }
}
