using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Schoolera.Application.Common.Interfaces;
using Schoolera.Domain.Entities;
using Schoolera.Domain.Enums;

namespace Schoolera.Infrastructure.Notifications;

public sealed class NotificationWorkerOptions
{
    public const string SectionName = "Notifications:Worker";

    public int BatchSize { get; set; } = 20;

    public int PollIntervalSeconds { get; set; } = 5;

    public int MaxAttempts { get; set; } = 8;

    public int StaleProcessingMinutes { get; set; } = 10;

    public int BaseRetrySeconds { get; set; } = 30;

    public int MaxRetrySeconds { get; set; } = 3600;
}

public sealed class NotificationOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationWorkerOptions> options,
    ILogger<NotificationOutboxWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> _rateWindows = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerOptions = options.Value;
        var pollDelay = TimeSpan.FromSeconds(Math.Max(1, workerOptions.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(workerOptions, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification outbox worker batch failed.");
            }

            try
            {
                await Task.Delay(pollDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(
        NotificationWorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var repository = services.GetRequiredService<INotificationRepository>();
        var providers = services.GetRequiredService<IEnumerable<INotificationChannelProvider>>()
            .ToDictionary(provider => provider.Channel);
        var integrationAccessor = services.GetRequiredService<IPlatformIntegrationConfigurationAccessor>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var staleBefore = DateTimeOffset.UtcNow.AddMinutes(-Math.Max(1, workerOptions.StaleProcessingMinutes));
        var batch = await repository.ClaimPendingBatchAsync(
            Math.Max(1, workerOptions.BatchSize),
            staleBefore,
            cancellationToken);

        if (batch.Count == 0)
        {
            return;
        }

        foreach (var message in batch)
        {
            await ProcessMessageAsync(
                message,
                workerOptions,
                providers,
                repository,
                integrationAccessor,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        NotificationOutboxMessage message,
        NotificationWorkerOptions workerOptions,
        IReadOnlyDictionary<NotificationChannel, INotificationChannelProvider> providers,
        INotificationRepository repository,
        IPlatformIntegrationConfigurationAccessor integrationAccessor,
        CancellationToken cancellationToken)
    {
        if (!TryAcquireRateLimit(message.RecipientUserId, message.Channel))
        {
            message.MarkSkipped("notifications.rateLimited");
            return;
        }

        if (!NotificationEventClassification.IsMandatory(message.EventType))
        {
            var preferences = await repository.GetPreferencesAsync(message.RecipientUserId, cancellationToken);
            if (preferences is null || !preferences.OptionalAdmissionsOpenEnabled)
            {
                message.MarkSkipped("notifications.optionalConsentMissing");
                return;
            }

            var channelEnabled = message.Channel switch
            {
                NotificationChannel.InApp => preferences.InAppEnabled,
                NotificationChannel.Email => preferences.EmailEnabled,
                NotificationChannel.Sms => preferences.SmsEnabled,
                NotificationChannel.WhatsApp => preferences.WhatsAppEnabled,
                _ => false,
            };

            if (!channelEnabled)
            {
                message.MarkSkipped("notifications.channelConsentMissing");
                return;
            }
        }

        if (!providers.TryGetValue(message.Channel, out var provider))
        {
            message.MarkDeadLetter("notifications.providerMissing");
            return;
        }

        ResolvedIntegrationConfiguration? integration = null;
        if (message.Channel != NotificationChannel.InApp)
        {
            var integrationType = message.Channel switch
            {
                NotificationChannel.Email => IntegrationType.Email,
                NotificationChannel.Sms => IntegrationType.Sms,
                NotificationChannel.WhatsApp => IntegrationType.WhatsApp,
                _ => (IntegrationType?)null,
            };

            if (integrationType is null)
            {
                message.MarkDeadLetter("notifications.unsupportedChannel");
                return;
            }

            integration = await integrationAccessor.GetActiveDefaultAsync(
                integrationType.Value,
                cancellationToken);

            if (integration is null)
            {
                message.MarkDeadLetter("notifications.integrationMissing");
                return;
            }

            if (!integration.IsConfigured &&
                !IntegrationProviderCodes.IsSimulated(integration.ProviderCode))
            {
                message.MarkDeadLetter("notifications.integrationConfigInvalid");
                return;
            }
        }

        NotificationSendResult result;
        try
        {
            result = await provider.SendAsync(message, integration, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Notification provider threw for message {MessageId} channel {Channel}.",
                message.Id,
                message.Channel);
            result = new NotificationSendResult(false, true, false, null, "notifications.providerException");
        }

        if (result.Skipped)
        {
            message.MarkSkipped(result.SafeFailureCode ?? "notifications.skipped");
            return;
        }

        if (result.Succeeded)
        {
            message.MarkSent(integration?.Id, result.ProviderMessageId);
            if (message.Channel == NotificationChannel.InApp)
            {
                message.MarkDelivered();
            }

            return;
        }

        var failureCode = result.SafeFailureCode ?? "notifications.sendFailed";
        if (!result.IsRetryable || message.AttemptCount >= workerOptions.MaxAttempts)
        {
            message.MarkDeadLetter(failureCode);
            return;
        }

        var delaySeconds = ComputeBackoffSeconds(
            message.AttemptCount,
            workerOptions.BaseRetrySeconds,
            workerOptions.MaxRetrySeconds);
        message.MarkFailed(failureCode, DateTimeOffset.UtcNow.AddSeconds(delaySeconds));
    }

    private bool TryAcquireRateLimit(Guid recipientUserId, NotificationChannel channel)
    {
        var key = $"{recipientUserId:N}:{(int)channel}";
        var queue = _rateWindows.GetOrAdd(key, _ => new ConcurrentQueue<DateTimeOffset>());
        var now = DateTimeOffset.UtcNow;
        var windowStart = now.AddHours(-1);

        while (queue.TryPeek(out var oldest) && oldest < windowStart)
        {
            queue.TryDequeue(out _);
        }

        if (queue.Count >= 20)
        {
            return false;
        }

        queue.Enqueue(now);
        return true;
    }

    private static int ComputeBackoffSeconds(int attemptCount, int baseRetrySeconds, int maxRetrySeconds)
    {
        var exponent = Math.Max(0, attemptCount - 1);
        var delay = baseRetrySeconds * Math.Pow(2, exponent);
        return (int)Math.Min(maxRetrySeconds, Math.Max(baseRetrySeconds, delay));
    }
}
