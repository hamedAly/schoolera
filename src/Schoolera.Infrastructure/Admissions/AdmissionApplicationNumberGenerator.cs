using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Admissions;

/// <summary>
/// Race-safe admission application number generator (APP-{yyyy}-{n:D6}).
/// Allocates the next value with an atomic SQL UPDATE/INSERT under UPDLOCK so shared
/// <see cref="SchooleraDbContext"/> change-tracker graphs (seed/admission flows) are never
/// detached or flushed as a side effect.
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
                // Ensure the year row exists (no-op if another allocator inserted it).
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    IF NOT EXISTS (
                        SELECT 1 FROM [AdmissionApplicationNumberSequences] WITH (UPDLOCK, HOLDLOCK)
                        WHERE [Year] = {year})
                    BEGIN
                        INSERT INTO [AdmissionApplicationNumberSequences] ([Year], [LastValue])
                        VALUES ({year}, 0);
                    END
                    """,
                    cancellationToken);

                long next;
                await using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                {
                    command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
                    command.CommandText =
                        """
                        UPDATE [AdmissionApplicationNumberSequences] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                        SET [LastValue] = [LastValue] + 1
                        OUTPUT INSERTED.[LastValue]
                        WHERE [Year] = @year;
                        """;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@year";
                    parameter.Value = year;
                    command.Parameters.Add(parameter);

                    if (command.Connection!.State != System.Data.ConnectionState.Open)
                    {
                        await command.Connection.OpenAsync(cancellationToken);
                    }

                    var scalar = await command.ExecuteScalarAsync(cancellationToken);
                    next = Convert.ToInt64(scalar);
                }

                // Keep any tracked sequence entity in sync so later SaveChanges does not rewrite it.
                var tracked = dbContext.ChangeTracker.Entries<AdmissionApplicationNumberSequence>()
                    .FirstOrDefault(entry => entry.Entity.Year == year);
                if (tracked is not null)
                {
                    tracked.Property(sequence => sequence.LastValue).CurrentValue = next;
                    tracked.State = EntityState.Unchanged;
                }

                if (ownsTransaction)
                {
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
