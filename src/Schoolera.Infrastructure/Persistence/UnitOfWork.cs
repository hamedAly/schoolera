using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Infrastructure.Persistence;

public sealed class UnitOfWork(SchooleraDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}