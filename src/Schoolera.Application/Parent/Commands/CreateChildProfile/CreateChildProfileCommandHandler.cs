using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Auth.Constants;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Parent.Common;
using Schoolera.Application.Parent.Constants;
using Schoolera.Application.Parent.Dtos;
using Schoolera.Application.Parent.Options;
using Schoolera.Application.Parent.Queries.GetParentChild;
using Schoolera.Application.Resources;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Parent.Commands.CreateChildProfile;

public sealed record CreateChildProfileCommand(CreateChildProfileRequest Body)
    : IRequest<Result<ChildProfileDetailDto>>;

public sealed class CreateChildProfileCommandHandler(
    ICurrentUser currentUser,
    IParentProfileRepository parentProfileRepository,
    IChildProfileRepository childProfileRepository,
    ITaxonomyRepository taxonomyRepository,
    IChildIdentityProtector identityProtector,
    IUnitOfWork unitOfWork,
    IOptions<ParentChildOptions> childOptions,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<CreateChildProfileCommandHandler> logger)
    : IRequestHandler<CreateChildProfileCommand, Result<ChildProfileDetailDto>>
{
    public async Task<Result<ChildProfileDetailDto>> Handle(
        CreateChildProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ChildProfileDetailDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var body = request.Body;
        if (body.HasSpecialNeeds is false && !string.IsNullOrWhiteSpace(body.SpecialNeedsNotes))
        {
            return Result<ChildProfileDetailDto>.Failure(
                ["Special-needs notes are only allowed when special needs is enabled."],
                [ParentErrorCodes.SpecialNeedsNotesNotAllowed]);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var opts = childOptions.Value;
        if (!ParentChildAge.IsSchoolAge(body.BirthDate, today, opts.MinAgeYears, opts.MaxAgeYears))
        {
            return Result<ChildProfileDetailDto>.Failure(
                ["Birth date is outside the allowed school-age range."],
                [ParentErrorCodes.InvalidBirthDate]);
        }

        var grade = await taxonomyRepository.GetGradeByIdAsync(body.CurrentGradeId, cancellationToken);
        if (grade is null || !grade.IsActive)
        {
            return Result<ChildProfileDetailDto>.Failure(
                ["Grade is invalid."],
                [ParentErrorCodes.InvalidGrade]);
        }

        var normalized = identityProtector.Normalize(body.IdentityValue);
        if (!identityProtector.IsValidFormat(body.IdentityType, normalized))
        {
            return Result<ChildProfileDetailDto>.Failure(
                ["Identity value format is invalid."],
                [ParentErrorCodes.InvalidIdentity]);
        }

        var lookupHash = identityProtector.ComputeLookupHash(normalized);
        if (await childProfileRepository.IdentityHashExistsAsync(userId, lookupHash, null, cancellationToken))
        {
            return Result<ChildProfileDetailDto>.Failure(
                ["A child with this identity already exists."],
                [ParentErrorCodes.IdentityAlreadyExists]);
        }

        var profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = new ParentProfile(userId);
            await parentProfileRepository.AddAsync(profile, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            profile = await parentProfileRepository.GetByUserIdAsync(userId, cancellationToken) ?? profile;
        }

        var child = new ChildProfile(
            profile.Id,
            userId,
            body.FullName,
            body.IdentityType,
            identityProtector.Protect(normalized),
            lookupHash,
            identityProtector.ExtractLastFour(normalized),
            body.BirthDate,
            body.Gender,
            body.CurrentGradeId,
            body.HasSpecialNeeds,
            body.SpecialNeedsNotes,
            body.CurrentSchoolName,
            body.PreferredStudyLanguage,
            body.Skills,
            body.Hobbies,
            body.Strengths,
            body.ImprovementAreas,
            body.HealthNotes);

        await childProfileRepository.AddAsync(child, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with grade for mapping.
        child = await childProfileRepository.GetOwnedAsync(userId, child.Id, cancellationToken) ?? child;
        logger.LogInformation("Created child {ChildId} for parent {UserId}.", child.Id, userId);

        return Result<ChildProfileDetailDto>.Success(
            GetParentChildQueryHandler.Map(child, identityProtector));
    }
}
