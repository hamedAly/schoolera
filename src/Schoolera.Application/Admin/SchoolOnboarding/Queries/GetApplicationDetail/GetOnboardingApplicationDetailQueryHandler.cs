using MediatR;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Application.Admin.SchoolOnboarding.Queries.GetApplicationDetail;

public sealed record GetOnboardingApplicationDetailQuery(Guid ApplicationId)
    : IRequest<Result<AdminOnboardingDetailDto>>;

public sealed class GetOnboardingApplicationDetailQueryHandler(
    ISchoolOnboardingRepository repository,
    IUserDirectory userDirectory,
    IStringLocalizer<OnboardingMessages> localizer)
    : IRequestHandler<GetOnboardingApplicationDetailQuery, Result<AdminOnboardingDetailDto>>
{
    public async Task<Result<AdminOnboardingDetailDto>> Handle(
        GetOnboardingApplicationDetailQuery request,
        CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(request.ApplicationId, includeChildren: true, cancellationToken);
        if (application is null)
        {
            return OnboardingResults.Failure<AdminOnboardingDetailDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        return Result<AdminOnboardingDetailDto>.Success(
            await OnboardingAdminReadModel.BuildAsync(application, repository, userDirectory, cancellationToken));
    }
}
