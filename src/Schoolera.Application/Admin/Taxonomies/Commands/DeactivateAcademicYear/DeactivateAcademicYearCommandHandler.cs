using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateAcademicYear;

public sealed record DeactivateAcademicYearCommand(Guid Id) : IRequest<Result<AcademicYearAdminDto>>;

public sealed class DeactivateAcademicYearCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateAcademicYearCommandHandler> logger)
    : IRequestHandler<DeactivateAcademicYearCommand, Result<AcademicYearAdminDto>>
{
    public async Task<Result<AcademicYearAdminDto>> Handle(
        DeactivateAcademicYearCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating academic year {AcademicYearId}.", request.Id);

        var academicYear = await taxonomyRepository.GetAcademicYearByIdAsync(request.Id, cancellationToken);
        if (academicYear is null)
        {
            return Result<AcademicYearAdminDto>.Failure(
                ["Academic year not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        academicYear.Update(
            academicYear.NameAr,
            academicYear.NameEn,
            academicYear.Slug,
            academicYear.StartDate,
            academicYear.EndDate,
            academicYear.IsCurrent,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicYearAdminDto>.Success(AcademicYearAdminDto.FromAcademicYear(academicYear));
    }
}
