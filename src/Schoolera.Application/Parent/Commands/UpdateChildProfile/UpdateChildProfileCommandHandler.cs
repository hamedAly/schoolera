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

namespace Schoolera.Application.Parent.Commands.UpdateChildProfile;

public sealed record UpdateChildProfileCommand(Guid ChildId, UpdateChildProfileRequest Body)
    : IRequest<Result<ChildProfileDetailDto>>;

public sealed class UpdateChildProfileCommandHandler(
    ICurrentUser currentUser,
    IChildProfileRepository childProfileRepository,
    ITaxonomyRepository taxonomyRepository,
    IChildIdentityProtector identityProtector,
    IUnitOfWork unitOfWork,
    IOptions<ParentChildOptions> childOptions,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UpdateChildProfileCommandHandler> logger)
    : IRequestHandler<UpdateChildProfileCommand, Result<ChildProfileDetailDto>>
{
    public async Task<Result<ChildProfileDetailDto>> Handle(
        UpdateChildProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || !currentUser.IsInRole(SchooleraRoles.Parent))
        {
            return Result<ChildProfileDetailDto>.Failure(
                [localizer["Forbidden"].Value],
                [ParentErrorCodes.Forbidden]);
        }

        var child = await childProfileRepository.GetOwnedForUpdateAsync(userId, request.ChildId, cancellationToken);
        if (child is null || !child.IsActive)
        {
            return Result<ChildProfileDetailDto>.Failure(
                ["Child not found."],
                [ParentErrorCodes.ChildNotFound]);
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

        var replaceIdentity = !string.IsNullOrWhiteSpace(body.IdentityValue);
        if (replaceIdentity)
        {
            if (body.IdentityType is null)
            {
                return Result<ChildProfileDetailDto>.Failure(
                    ["Identity type is required when replacing identity."],
                    [ParentErrorCodes.InvalidIdentity]);
            }

            var normalized = identityProtector.Normalize(body.IdentityValue!);
            if (!identityProtector.IsValidFormat(body.IdentityType.Value, normalized))
            {
                return Result<ChildProfileDetailDto>.Failure(
                    ["Identity value format is invalid."],
                    [ParentErrorCodes.InvalidIdentity]);
            }

            var lookupHash = identityProtector.ComputeLookupHash(normalized);
            if (await childProfileRepository.IdentityHashExistsAsync(
                    userId,
                    lookupHash,
                    child.Id,
                    cancellationToken))
            {
                return Result<ChildProfileDetailDto>.Failure(
                    ["A child with this identity already exists."],
                    [ParentErrorCodes.IdentityAlreadyExists]);
            }

            child.ReplaceIdentity(
                body.IdentityType.Value,
                identityProtector.Protect(normalized),
                lookupHash,
                identityProtector.ExtractLastFour(normalized));
        }

        child.UpdateWithoutIdentity(
            body.FullName,
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

        await unitOfWork.SaveChangesAsync(cancellationToken);
        child = await childProfileRepository.GetOwnedAsync(userId, child.Id, cancellationToken) ?? child;
        logger.LogInformation("Updated child {ChildId} for parent {UserId}.", child.Id, userId);

        return Result<ChildProfileDetailDto>.Success(
            GetParentChildQueryHandler.Map(child, identityProtector));
    }
}
