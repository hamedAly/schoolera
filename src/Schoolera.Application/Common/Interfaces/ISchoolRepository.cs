using Schoolera.Domain.Entities;

namespace Schoolera.Application.Common.Interfaces;

public interface ISchoolRepository
{
    Task AddAsync(School school, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<School>> ListAsync(CancellationToken cancellationToken = default);
}