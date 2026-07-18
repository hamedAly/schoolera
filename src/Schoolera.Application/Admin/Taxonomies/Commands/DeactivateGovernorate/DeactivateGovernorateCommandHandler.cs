using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Admin.Taxonomies.Dtos;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Common.Models;

namespace Schoolera.Application.Admin.Taxonomies.Commands.DeactivateGovernorate;

public sealed record DeactivateGovernorateCommand(Guid Id) : IRequest<Result<GovernorateAdminDto>>;

public sealed class DeactivateGovernorateCommandHandler(
    ITaxonomyRepository taxonomyRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeactivateGovernorateCommandHandler> logger)
    : IRequestHandler<DeactivateGovernorateCommand, Result<GovernorateAdminDto>>
{
    public async Task<Result<GovernorateAdminDto>> Handle(
        DeactivateGovernorateCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deactivating governorate {GovernorateId}.", request.Id);

        var governorate = await taxonomyRepository.GetGovernorateByIdAsync(request.Id, cancellationToken);
        if (governorate is null)
        {
            return Result<GovernorateAdminDto>.Failure(
                ["Governorate not found."],
                [TaxonomyErrorCodes.NotFound]);
        }

        governorate.Update(
            governorate.CountryId,
            governorate.NameAr,
            governorate.NameEn,
            governorate.Slug,
            governorate.SortOrder,
            isActive: false);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GovernorateAdminDto>.Success(GovernorateAdminDto.FromGovernorate(governorate));
    }
}
