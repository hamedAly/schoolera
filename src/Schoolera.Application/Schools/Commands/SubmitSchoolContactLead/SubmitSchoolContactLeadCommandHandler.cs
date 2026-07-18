using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Domain.Entities;
using System.Globalization;

namespace Schoolera.Application.Schools.Commands.SubmitSchoolContactLead;

public sealed record SubmitSchoolContactLeadCommand(string Slug, SchoolContactLeadRequest Body)
    : IRequest<Result<SchoolContactLeadResultDto>>;

public sealed class SubmitSchoolContactLeadCommandHandler(
    ISchoolReadRepository schoolReadRepository,
    ISchoolContactLeadRepository contactLeadRepository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<AuthMessages> localizer,
    ILogger<SubmitSchoolContactLeadCommandHandler> logger)
    : IRequestHandler<SubmitSchoolContactLeadCommand, Result<SchoolContactLeadResultDto>>
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(2);

    public async Task<Result<SchoolContactLeadResultDto>> Handle(
        SubmitSchoolContactLeadCommand request,
        CancellationToken cancellationToken)
    {
        var school = await schoolReadRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);
        if (school is null)
        {
            return Result<SchoolContactLeadResultDto>.Failure(
                [localizer["AdminSchoolNotFound"].Value],
                [SchoolErrorCodes.NotFound]);
        }

        var source = SchoolContactLeadSources.Normalize(request.Body.Source)!;
        var phone = request.Body.Phone.Trim();

        if (await contactLeadRepository.HasRecentDuplicateAsync(
                school.Id,
                phone,
                DuplicateWindow,
                cancellationToken))
        {
            logger.LogWarning(
                "Rejected duplicate contact lead for school {SchoolId} within cooldown window.",
                school.Id);

            return Result<SchoolContactLeadResultDto>.Failure(
                [localizer["ContactLeadRejected"].Value],
                [SchoolErrorCodes.ContactRejected]);
        }

        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var lead = new SchoolContactLead(
            school.Id,
            source,
            request.Body.Name,
            phone,
            request.Body.Email,
            request.Body.Message,
            culture);

        await contactLeadRepository.AddAsync(lead, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stored contact lead {LeadId} for school {SchoolId} from source {Source}.",
            lead.Id,
            school.Id,
            source);

        return Result<SchoolContactLeadResultDto>.Success(
            new SchoolContactLeadResultDto(
                lead.Id,
                lead.CreatedAtUtc,
                localizer["ContactLeadSubmitted"].Value));
    }
}
