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

namespace Schoolera.Application.Admissions.Commands.DeleteAdmissionAnswer;

public sealed record DeleteAdmissionAnswerCommand(
    Guid ApplicationId,
    Guid QuestionSnapshotId)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class DeleteAdmissionAnswerCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<DeleteAdmissionAnswerCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<DeleteAdmissionAnswerCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        DeleteAdmissionAnswerCommand request,
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

        var answer = await admissionRepository.GetOwnedAnswerForUpdateAsync(
            userId,
            request.ApplicationId,
            request.QuestionSnapshotId,
            cancellationToken);
        if (answer is null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Question snapshot not found.",
                AdmissionErrorCodes.QuestionSnapshotNotFound);
        }

        admissionRepository.RemoveAnswer(answer);

        var conflict = await AdmissionResults.TrySaveAsync<AdmissionApplicationDetailDto>(
            unitOfWork,
            cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        var result = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        logger.LogInformation(
            "Deleted admission answer for snapshot {SnapshotId} on application {ApplicationId}.",
            request.QuestionSnapshotId,
            application.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(result, identityProtector));
    }
}
