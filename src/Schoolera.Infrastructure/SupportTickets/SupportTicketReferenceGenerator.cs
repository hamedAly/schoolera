using Microsoft.EntityFrameworkCore;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.SupportTickets;

/// <summary>
/// Race-safe ticket reference generator (ST-{yyyyMMdd}-{n:D5}).
/// </summary>
public sealed class SupportTicketReferenceGenerator(SchooleraDbContext dbContext)
    : ISupportTicketReferenceGenerator
{
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            var dayKey = DateTime.UtcNow.ToString("yyyyMMdd");
            var ownsTransaction = dbContext.Database.CurrentTransaction is null;

            if (ownsTransaction)
            {
                await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                var sequence = await dbContext.SupportTicketNumberSequences
                    .FromSqlInterpolated(
                        $"""
                        SELECT * FROM [SupportTicketNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                        WHERE [DayKey] = {dayKey}
                        """)
                    .AsTracking()
                    .SingleOrDefaultAsync(cancellationToken);

                if (sequence is null)
                {
                    sequence = new SupportTicketNumberSequence(dayKey);
                    dbContext.SupportTicketNumberSequences.Add(sequence);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var next = sequence.Next();

                if (ownsTransaction)
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await dbContext.Database.CommitTransactionAsync(cancellationToken);
                }

                return $"ST-{dayKey}-{next:D5}";
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
