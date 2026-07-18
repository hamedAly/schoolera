using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;

namespace Schoolera.Infrastructure.Persistence.Repositories;

public sealed class SchoolRepository(SchooleraDbContext dbContext) : ISchoolRepository
{
    public async Task AddAsync(School school, CancellationToken cancellationToken = default)
    {
        await dbContext.Schools.AddAsync(school, cancellationToken);
    }

    public async Task<IReadOnlyCollection<School>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Schools
            .AsNoTracking()
            .OrderBy(school => school.NameAr)
            .ToArrayAsync(cancellationToken);
    }
}
