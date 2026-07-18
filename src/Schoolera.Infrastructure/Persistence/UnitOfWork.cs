using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Exceptions;
using Schoolera.Application.Common.Interfaces;

namespace Schoolera.Infrastructure.Persistence;

public sealed class UnitOfWork(SchooleraDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "concurrency.conflict",
                "The record was modified by another operation.",
                exception);
        }
    }
}
