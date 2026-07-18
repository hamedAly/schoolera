using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Application.Resources;
using Schoolera.Application.SchoolPortal.Common;
using Schoolera.Application.SchoolPortal.Auth;
using Schoolera.Application.SchoolPortal.Constants;
using Schoolera.Application.SchoolPortal.Dtos;

namespace Schoolera.Application.SchoolPortal.Commands.UpdateSchoolPortalProfile;

public sealed record UpdateSchoolPortalProfileCommand(
    Guid SchoolId,
    UpdateSchoolPortalProfileRequest Body) : IRequest<Result<SchoolPortalProfileDto>>;

public sealed class UpdateSchoolPortalProfileCommandHandler(
    ISchoolPortalAccess portalAccess,
    ISchoolPortalRepository repository,
    IUnitOfWork unitOfWork,
    IStringLocalizer<SchoolPortalMessages> localizer,
    ILogger<UpdateSchoolPortalProfileCommandHandler> logger)
    : IRequestHandler<UpdateSchoolPortalProfileCommand, Result<SchoolPortalProfileDto>>
{
    public async Task<Result<SchoolPortalProfileDto>> Handle(
        UpdateSchoolPortalProfileCommand request,
        CancellationToken cancellationToken)
    {
        var accessResult = await portalAccess.ResolveAsync(request.SchoolId, cancellationToken);
        if (!accessResult.Succeeded || accessResult.Data is null)
        {
            return Result<SchoolPortalProfileDto>.Failure(accessResult.Errors, accessResult.ErrorCodes);
        }
        var permissionCheck = SchoolPortalAccess.RequireEditablePermission<SchoolPortalProfileDto>(
            accessResult.Data, SchoolPortalPermission.ManageProfile, localizer);
        if (!permissionCheck.Succeeded)
        {
            return permissionCheck;
        }

        var school = await repository.GetSchoolForWriteAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return SchoolPortalResults.FailureForCode<SchoolPortalProfileDto>(
                localizer, SchoolPortalErrorCodes.SchoolNotFound);
        }

        var body = request.Body;
        school.UpdatePortalProfile(
            body.NameAr,
            body.NameEn,
            body.ShortDescriptionAr,
            body.ShortDescriptionEn,
            body.FullDescriptionAr,
            body.FullDescriptionEn,
            body.SchoolType,
            body.GenderType,
            body.FoundedYear,
            body.StudentCount,
            body.PublicPhone,
            body.PublicEmail,
            body.WebsiteUrl,
            body.WhatsAppNumber,
            body.SeoTitleAr,
            body.SeoTitleEn,
            body.SeoDescriptionAr,
            body.SeoDescriptionEn);

        var conflict = await SchoolPortalResults.TrySaveAsync<SchoolPortalProfileDto>(
            unitOfWork, localizer, cancellationToken);
        if (conflict is not null)
        {
            return conflict;
        }

        return Result<SchoolPortalProfileDto>.Success(SchoolPortalReadModel.ToProfile(school));
    }
}
