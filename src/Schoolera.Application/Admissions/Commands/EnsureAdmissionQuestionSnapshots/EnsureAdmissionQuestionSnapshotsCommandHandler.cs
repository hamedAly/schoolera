using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;

namespace Schoolera.Application.Admissions.Commands.EnsureAdmissionQuestionSnapshots;

public sealed record EnsureAdmissionQuestionSnapshotsCommand(Guid ApplicationId)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class EnsureAdmissionQuestionSnapshotsCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<EnsureAdmissionQuestionSnapshotsCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<EnsureAdmissionQuestionSnapshotsCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        EnsureAdmissionQuestionSnapshotsCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return AdmissionResults.Forbidden<AdmissionApplicationDetailDto>(localizer);
        }

        var application = await admissionRepository.GetOwnedForUpdateAsync(
            userId,
            request.ApplicationId,
            cancellationToken);
        if (application is null)
        {
            return AdmissionResults.NotFound<AdmissionApplicationDetailDto>();
        }

        if (!AdmissionTransitionPolicy.CanParentEdit(application.Status))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Admission application is read-only.",
                AdmissionErrorCodes.ReadOnly);
        }

        await questionSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken: cancellationToken);

        var conflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Ensured question snapshots for admission application {ApplicationId}.",
            application.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(loaded, identityProtector));
    }
}
