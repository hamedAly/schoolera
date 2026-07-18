using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Meetings;

namespace Schoolera.Infrastructure.Meetings;

public sealed class MeetingSessionWorkerOptions
{
    public const string SectionName = "Meetings:Worker";
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
}

public sealed class MeetingSessionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MeetingSessionWorkerOptions> options,
    ILogger<MeetingSessionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        var delay = TimeSpan.FromSeconds(Math.Max(2, options.Value.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IMeetingSessionService>();
                await service.ProcessDueSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Meeting session worker batch failed.");
            }
            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
