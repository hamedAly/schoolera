using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolOnboarding.Common;
using Schoolera.Application.SchoolOnboarding.Constants;
using Schoolera.Application.SchoolOnboarding.Dtos;

namespace Schoolera.Application.SchoolOnboarding.Commands.DeleteDocument;

public sealed record DeleteOnboardingDocumentCommand(Guid DocumentId)
    : IRequest<Result<MyOnboardingApplicationDto>>;

public sealed class DeleteOnboardingDocumentCommandHandler(
    ISchoolOnboardingRepository repository,
    IPrivateFileStorage privateFileStorage,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IStringLocalizer<OnboardingMessages> localizer,
    ILogger<DeleteOnboardingDocumentCommandHandler> logger)
    : IRequestHandler<DeleteOnboardingDocumentCommand, Result<MyOnboardingApplicationDto>>
{
    public async Task<Result<MyOnboardingApplicationDto>> Handle(
        DeleteOnboardingDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = OnboardingOwnerAccess.ResolveOwnerId(currentUser);
        if (ownerId is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "OwnerRoleRequired", OnboardingErrorCodes.OwnerRoleRequired);
        }

        var application = await repository.GetByOwnerAsync(ownerId.Value, includeChildren: true, cancellationToken);
        if (application is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        if (!application.IsEditable)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "NotEditable", OnboardingErrorCodes.NotEditable);
        }

        var document = application.Documents
            .FirstOrDefault(candidate => candidate.Id == request.DocumentId && candidate.IsCurrent);
        if (document is null)
        {
            return OnboardingResults.Failure<MyOnboardingApplicationDto>(
                localizer, "NotFound", OnboardingErrorCodes.NotFound);
        }

        document.Retire(DateTimeOffset.UtcNow);

        var conflict = await OnboardingResults.TrySaveAsync<MyOnboardingApplicationDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        await privateFileStorage.DeleteAsync(document.StoredFileReference, cancellationToken);

        logger.LogInformation(
            "Removed onboarding document {DocumentId} from application {ApplicationId}.",
            document.Id,
            application.Id);

        return Result<MyOnboardingApplicationDto>.Success(
            await OnboardingReadModel.BuildAsync(application, repository, cancellationToken));
    }
}
