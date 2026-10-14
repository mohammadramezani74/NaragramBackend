using CleanArchitecture.Application.Bots;
using CleanArchitecture.Application.Bots.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.BackgroundJob
{
    /// <summary>
    /// صف ربات‌ها را می‌خواند و هر اتفاق را در یک scope تازه به موتور می‌دهد.
    /// هر بخش صف یک حلقه‌ی جدا دارد: پیام‌های یک کاربر به ترتیب، کاربرهای
    /// مختلف هم‌زمان. خطای یک اتفاق فقط لاگ می‌شود و بقیه ادامه پیدا می‌کنند.
    /// </summary>
    internal sealed class BotDispatcherService(
        IBotUpdateQueue queue,
        IServiceScopeFactory scopes,
        ILogger<BotDispatcherService> logger) : BackgroundService
    {
        private static readonly TimeSpan MaxPerUpdate = TimeSpan.FromSeconds(60);

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
            => Task.WhenAll(Enumerable.Range(0, queue.PartitionCount).Select(p => RunPartitionAsync(p, stoppingToken)));

        private async Task RunPartitionAsync(int partition, CancellationToken stoppingToken)
        {
            await Task.Yield();
            try
            {
                await foreach (var update in queue.ReadAllAsync(partition, stoppingToken))
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeout.CancelAfter(MaxPerUpdate);
                    try
                    {
                        using var scope = scopes.CreateScope();
                        var engine = scope.ServiceProvider.GetRequiredService<IBotEngine>();
                        await engine.HandleAsync(update, timeout.Token);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Bot update failed: {Kind} bot {Bot} user {User}", update.Kind, update.BotUserId, update.UserId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
