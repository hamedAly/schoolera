using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

/// <summary>
/// Race-safe admission application number generator (APP-{yyyy}-{n:D6}).
/// Locks the year row with SQL Server UPDLOCK/ROWLOCK/HOLDLOCK inside an execution-strategy
/// transaction. When no ambient transaction exists, this class begins and commits its own so the
/// increment is durable before the number is returned. When an ambient transaction is present
/// (e.g. a future UnitOfWork transaction around SaveChanges), the sequence entity stays tracked
/// and is persisted with that ambient SaveChanges — the lock is held until the outer commit.
/// </summary>
public sealed class AdmissionApplicationNumberGenerator(SchooleraDbContext dbContext)
    : IAdmissionApplicationNumberGenerator
{
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            var year = DateTime.UtcNow.Year;
            var ownsTransaction = dbContext.Database.CurrentTransaction is null;

            if (ownsTransaction)
            {
                await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var sequence = await dbContext.AdmissionApplicationNumberSequences
                    .FromSqlInterpolated(
                        $"""
                        SELECT * FROM [AdmissionApplicationNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                        WHERE [Year] = {year}
                        """)
                    .AsTracking()
                    .SingleOrDefaultAsync(cancellationToken);

                if (sequence is null)
                {
                    sequence = new AdmissionApplicationNumberSequence(year);
                    dbContext.AdmissionApplicationNumberSequences.Add(sequence);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var next = sequence.Next();

                if (ownsTransaction)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await dbContext.Database.CommitTransactionAsync(cancellationToken);
                }

                return $"APP-{year}-{next:D6}";
            }
            catch
            {
                if (ownsTransaction && dbContext.Database.CurrentTransaction is not null)
                {
                    await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                }

                throw;
            }
        });
    }
}
