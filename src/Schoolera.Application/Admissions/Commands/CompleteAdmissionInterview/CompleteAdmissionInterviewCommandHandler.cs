using Microsoft.Extensions.Localization;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admissions.Common;
using Schoolera.Application.Admissions.Constants;
using Schoolera.Application.Admissions.Dtos;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.CompleteAdmissionInterview;

public sealed record CompleteAdmissionInterviewCommand(
    Guid SchoolId, Guid ApplicationId, CompleteAdmissionAppointmentRequest Body)
    : IRequest<Result<SchoolAdmissionApplicationDetailDto>>;

public sealed class CompleteAdmissionInterviewCommandHandler(
    ISchoolPortalAccess portalAccess,
    IAdmissionApplicationRepository admissionRepository,
    IUserDirectory userDirectory,
    IUnitOfWork unitOfWork,
    IChildIdentityProtector identityProtector,
    IInterviewAssessmentSlotRepository slotRepository,
    ILogger<CompleteAdmissionInterviewCommandHandler> logger,
    IStringLocalizer<SchoolPortalMessages> localizer)
    : IRequestHandler<CompleteAdmissionInterviewCommand, Result<SchoolAdmissionApplicationDetailDto>>
{
    public async Task<Result<SchoolAdmissionApplicationDetailDto>> Handle(
        CompleteAdmissionInterviewCommand request,
        CancellationToken cancellationToken)
    {
        var begin = await SchoolLifecycleCommandSupport.BeginAsync(
            portalAccess,
            admissionRepository,
            localizer,
            request.SchoolId,
            request.ApplicationId,
            request.Body.RowVersion, cancellationToken);
        if (begin.EarlyResult is not null)
        {
            return begin.EarlyResult;
        }

        return AdmissionResults.Failure<SchoolAdmissionApplicationDetailDto>(
            "The interview must be completed by finalizing its evaluation result.",
            AdmissionErrorCodes.EvaluationInvalidTransition);
    }
}
