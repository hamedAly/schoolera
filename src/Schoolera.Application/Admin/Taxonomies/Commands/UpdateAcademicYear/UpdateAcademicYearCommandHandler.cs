using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.UpdateAcademicYear;

public sealed record UpdateAcademicYearCommand(
    Guid Id,
    string NameAr,
    string? NameEn,
    string Slug,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent,
    bool IsActive) : IRequest<Result<AcademicYearAdminDto>>;

public sealed class UpdateAcademicYearCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateAcademicYearCommandHandler> logger)
    : IRequestHandler<UpdateAcademicYearCommand, Result<AcademicYearAdminDto>>
{
    public async Task<Result<AcademicYearAdminDto>> Handle(
        UpdateAcademicYearCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating academic year {AcademicYearId}.", request.Id);

        var academicYear = await taxonomyRepository.GetAcademicYearByIdAsync(request.Id, cancellationToken);
        if (academicYear is null)
        {
            return Result<AcademicYearAdminDto>.Failure(
                ["Academic year not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        if (await taxonomyRepository.IsAcademicYearSlugTakenAsync(
                request.Slug,
                request.Id,
                cancellationToken))
        {
            return Result<AcademicYearAdminDto>.Failure(
                ["Academic year slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        academicYear.Update(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.StartDate,
            request.EndDate,
            request.IsCurrent,
            request.IsActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicYearAdminDto>.Success(AcademicYearAdminDto.FromAcademicYear(academicYear));
    }
}
