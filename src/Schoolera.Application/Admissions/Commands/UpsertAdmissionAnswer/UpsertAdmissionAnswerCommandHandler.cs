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
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Application.Admissions.Commands.UpsertAdmissionAnswer;

public sealed record UpsertAdmissionAnswerCommand(
    Guid ApplicationId,
    UpsertAdmissionAnswerRequest Body)
    : IRequest<Result<AdmissionApplicationDetailDto>>;

public sealed class UpsertAdmissionAnswerCommandHandler(
    ICurrentUser currentUser,
    IAdmissionApplicationRepository admissionRepository,
    IAdmissionQuestionSnapshotService questionSnapshotService,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<UpsertAdmissionAnswerCommandHandler> logger,
    IChildIdentityProtector identityProtector)
    : IRequestHandler<UpsertAdmissionAnswerCommand, Result<AdmissionApplicationDetailDto>>
{
    public async Task<Result<AdmissionApplicationDetailDto>> Handle(
        UpsertAdmissionAnswerCommand request,
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

        if (!AdmissionTransitionPolicy.CanParentEdit(application.Status) &&
            !AdmissionTransitionPolicy.CanParentEditRequestedItems(application.Status))
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Admission application is read-only.",
                AdmissionErrorCodes.ReadOnly);
        }

        if (application.Status == AdmissionApplicationStatus.MissingDocuments)
        {
            var requested = application.ActiveMissingItemsRequest?.Items
                .Any(item =>
                    item.Kind == AdmissionMissingItemKind.QuestionSnapshot &&
                    item.QuestionSnapshotId == request.Body.QuestionSnapshotId) == true;
            if (!requested)
            {
                return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                    "Question was not requested for correction.",
                    AdmissionErrorCodes.MissingItemNotRequested);
            }
        } else {
            await questionSnapshotService.EnsureSnapshotsAsync(application, userId, cancellationToken: cancellationToken);
        }

        var loaded = await admissionRepository.GetOwnedAsync(userId, application.Id, cancellationToken)
            ?? application;
        var snapshot = (application.Status == AdmissionApplicationStatus.MissingDocuments
                ? application.QuestionSnapshots
                : loaded.QuestionSnapshots)
            .FirstOrDefault(item => item.Id == request.Body.QuestionSnapshotId);
        if (snapshot is null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Question snapshot not found.",
                AdmissionErrorCodes.QuestionSnapshotNotFound);
        }

        var existing = await admissionRepository.GetOwnedAnswerForUpdateAsync(
            userId,
            application.Id,
            request.Body.QuestionSnapshotId,
            cancellationToken);

        var answer = existing ?? new AdmissionApplicationAnswer(application.Id, snapshot.Id);
        var errorCode = ApplyAnswer(snapshot, answer, request.Body, loaded.Attachments);
        if (errorCode is not null)
        {
            return AdmissionResults.Failure<AdmissionApplicationDetailDto>(
                "Question answer is invalid.",
                errorCode);
        }

        if (existing is null)
        {
            admissionRepository.AddAnswer(answer);
        }

        if (application.Status == AdmissionApplicationStatus.MissingDocuments)
        {
            application.ActiveMissingItemsRequest?.Items
                .FirstOrDefault(item =>
                    item.Kind == AdmissionMissingItemKind.QuestionSnapshot &&
                    item.QuestionSnapshotId == snapshot.Id)
                ?.MarkCompleted();
        }

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
            "Upserted admission answer for snapshot {SnapshotId} on application {ApplicationId}.",
            snapshot.Id,
            application.Id);

        return Result<AdmissionApplicationDetailDto>.Success(
            AdmissionMapping.ToDetail(result, identityProtector));
    }

    private static string? ApplyAnswer(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer answer,
        UpsertAdmissionAnswerRequest body,
        IEnumerable<AdmissionApplicationAttachment> attachments)
    {
        return snapshot.QuestionType switch
        {
            AdmissionQuestionType.ShortText or AdmissionQuestionType.LongText =>
                ApplyText(snapshot, answer, body.TextValue),

            AdmissionQuestionType.SingleChoice =>
                body.SelectedOptionCodes is { Count: > 0 }
                    ? TrySet(() => answer.SetSingleChoice(body.SelectedOptionCodes[0]))
                    : AdmissionErrorCodes.QuestionAnswerInvalid,

            AdmissionQuestionType.MultipleChoice =>
                body.SelectedOptionCodes is { Count: > 0 }
                    ? TrySet(() => answer.SetMultipleChoice(body.SelectedOptionCodes))
                    : AdmissionErrorCodes.QuestionAnswerInvalid,

            AdmissionQuestionType.Date =>
                body.DateValue is { } date
                    ? TrySet(() => answer.SetDate(date))
                    : AdmissionErrorCodes.QuestionAnswerInvalid,

            AdmissionQuestionType.YesNo =>
                body.BooleanValue is { } value
                    ? TrySet(() => answer.SetYesNo(value))
                    : AdmissionErrorCodes.QuestionAnswerInvalid,

            AdmissionQuestionType.File =>
                ApplyFile(snapshot, answer, body.AttachmentId, attachments),

            _ => AdmissionErrorCodes.QuestionAnswerInvalid,
        };
    }

    private static string? ApplyText(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer answer,
        string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            answer.Clear();
            return null;
        }

        var trimmed = text.Trim();
        if (snapshot.MinLength is { } min && trimmed.Length < min)
        {
            return AdmissionErrorCodes.QuestionAnswerInvalid;
        }

        var max = snapshot.MaxLength ?? AdmissionQuestionCatalog.DefaultMaxLength(snapshot.QuestionType);
        if (trimmed.Length > max)
        {
            return AdmissionErrorCodes.QuestionAnswerInvalid;
        }

        answer.SetText(trimmed);
        return null;
    }

    private static string? ApplyFile(
        AdmissionApplicationQuestionSnapshot snapshot,
        AdmissionApplicationAnswer answer,
        Guid? attachmentId,
        IEnumerable<AdmissionApplicationAttachment> attachments)
    {
        if (attachmentId is not { } id)
        {
            answer.Clear();
            return null;
        }

        var attachment = attachments.FirstOrDefault(item => item.Id == id);
        if (attachment is null || attachment.QuestionSnapshotId != snapshot.Id)
        {
            return AdmissionErrorCodes.QuestionAnswerInvalid;
        }

        var allowed = AdmissionRequirementCatalog.ParseExtensions(snapshot.AllowedFileExtensions);
        var extension = Path.GetExtension(attachment.OriginalFileName);
        if (allowed.Count > 0 &&
            !allowed.Contains(AdmissionRequirementCatalog.NormalizeExtension(extension)))
        {
            return AdmissionErrorCodes.QuestionFileRules;
        }

        if (snapshot.MaxFileSizeBytes is { } max && attachment.FileSizeBytes > max)
        {
            return AdmissionErrorCodes.QuestionFileRules;
        }

        answer.SetFile(id);
        return null;
    }

    private static string? TrySet(Action setter)
    {
        try
        {
            setter();
            return null;
        }
        catch (ArgumentException)
        {
            return AdmissionErrorCodes.QuestionAnswerInvalid;
        }
    }
}
