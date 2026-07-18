using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Infrastructure.Persistence;

namespace Schoolera.Infrastructure.Analytics;

/// <summary>
/// Bounded in-process queue for school profile view aggregates.
/// Capacity 2,000; DropWrite when full (logged without visitor identity).
/// </summary>
public sealed class SchoolProfileViewQueue : ISchoolProfileViewQueue
{
    private readonly Channel<SchoolProfileViewEvent> _channel;
    private readonly ILogger<SchoolProfileViewQueue> _logger;

    public SchoolProfileViewQueue(ILogger<SchoolProfileViewQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<SchoolProfileViewEvent>(
            new BoundedChannelOptions(2000)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false,
            });
    }

    public ChannelReader<SchoolProfileViewEvent> Reader => _channel.Reader;

    public bool TryEnqueue(Guid schoolId, DateTimeOffset viewedAtUtc)
    {
        var written = _channel.Writer.TryWrite(new SchoolProfileViewEvent(schoolId, viewedAtUtc));
        if (!written)
        {
            _logger.LogWarning(
                "Dropped school profile view aggregate event for school {SchoolId}; analytics queue is full.",
                schoolId);
        }

        return written;
    }

    public void Complete() => _channel.Writer.TryComplete();
}

/// <summary>
/// Hosted worker that upserts <c>SchoolProfileViewDaily</c> rows. Creates its own DI scope per batch.
/// Best-effort in-process analytics — not guaranteed distributed delivery.
/// </summary>
public sealed class SchoolProfileViewBackgroundService(
    SchoolProfileViewQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<SchoolProfileViewBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in ReadBatchesAsync(stoppingToken))
        {
            try
            {
                await PersistBatchAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to persist {Count} school profile view aggregate events.",
                    batch.Count);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Complete();
        await base.StopAsync(cancellationToken);

        // Best-effort drain of remaining items during graceful shutdown.
        var remaining = new List<SchoolProfileViewEvent>();
        while (queue.Reader.TryRead(out var item))
        {
            remaining.Add(item);
        }

        if (remaining.Count > 0)
        {
            try
            {
                await PersistBatchAsync(remaining, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not drain {Count} school profile view events during shutdown.",
                    remaining.Count);
            }
        }
    }

    private async IAsyncEnumerable<IReadOnlyList<SchoolProfileViewEvent>> ReadBatchesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new List<SchoolProfileViewEvent>(32);
        await foreach (var item in queue.Reader.ReadAllAsync(cancellationToken))
        {
            buffer.Add(item);
            while (buffer.Count < 32 && queue.Reader.TryRead(out var extra))
            {
                buffer.Add(extra);
            }

            yield return buffer.ToArray();
            buffer.Clear();
        }
    }

    private async Task PersistBatchAsync(
        IReadOnlyList<SchoolProfileViewEvent> batch,
        CancellationToken cancellationToken)
    {
        var aggregates = batch
            .GroupBy(item => (item.SchoolId, ViewDate: DateOnly.FromDateTime(item.ViewedAtUtc.UtcDateTime)))
            .Select(group => (group.Key.SchoolId, group.Key.ViewDate, Count: group.Count()))
            .ToArray();

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchooleraDbContext>();

        foreach (var (schoolId, viewDate, count) in aggregates)
        {
            // Atomic upsert appropriate for SQL Server; avoids visitor identity storage.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 MERGE INTO [SchoolProfileViewDaily] AS target
                 USING (SELECT {schoolId} AS [SchoolId], {viewDate} AS [ViewDateUtc]) AS source
                 ON target.[SchoolId] = source.[SchoolId] AND target.[ViewDateUtc] = source.[ViewDateUtc]
                 WHEN MATCHED THEN
                     UPDATE SET [ViewCount] = target.[ViewCount] + {count}, [UpdatedAtUtc] = SYSUTCDATETIME()
                 WHEN NOT MATCHED THEN
                     INSERT ([Id], [SchoolId], [ViewDateUtc], [ViewCount], [UpdatedAtUtc])
                     VALUES (NEWID(), source.[SchoolId], source.[ViewDateUtc], {count}, SYSUTCDATETIME());
                 """,
                cancellationToken);
        }
    }
}
