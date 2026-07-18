using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Admin.Taxonomies.Commands.CreateAcademicYear;

public sealed record CreateAcademicYearCommand(
    string NameAr,
    string? NameEn,
    string Slug,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent) : IRequest<Result<AcademicYearAdminDto>>;

public sealed class CreateAcademicYearCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateAcademicYearCommandHandler> logger)
    : IRequestHandler<CreateAcademicYearCommand, Result<AcademicYearAdminDto>>
{
    public async Task<Result<AcademicYearAdminDto>> Handle(
        CreateAcademicYearCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating academic year {Slug}.", request.Slug);

        if (await taxonomyRepository.IsAcademicYearSlugTakenAsync(request.Slug, cancellationToken: cancellationToken))
        {
            return Result<AcademicYearAdminDto>.Failure(
                ["Academic year slug is already taken."],
                [TaxonomyErrorCodes.SlugDuplicate]);
        }

        var academicYear = new AcademicYear(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.StartDate,
            request.EndDate,
            request.IsCurrent);
        await taxonomyRepository.AddAcademicYearAsync(academicYear, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AcademicYearAdminDto>.Success(AcademicYearAdminDto.FromAcademicYear(academicYear));
    }
}
