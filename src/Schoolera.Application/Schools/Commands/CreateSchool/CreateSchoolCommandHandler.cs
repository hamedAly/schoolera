using MediatR;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Application.Schools.Dtos;
using Schoolera.Domain.Entities;

namespace Schoolera.Application.Schools.Commands.CreateSchool;

public sealed record CreateSchoolCommand(string Name, string? City) : IRequest<SchoolDto>;

public sealed class CreateSchoolCommandHandler(
    ISchoolRepository schoolRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateSchoolCommandHandler> logger) : IRequestHandler<CreateSchoolCommand, SchoolDto>
{
    public async Task<SchoolDto> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating school {SchoolName}.", request.Name);

        var school = new School(request.Name, request.City);

        await schoolRepository.AddAsync(school, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SchoolDto.FromEntity(school);
    }
}