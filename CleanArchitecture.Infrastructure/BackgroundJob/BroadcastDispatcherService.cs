using CleanArchitecture.Application.AdminPanel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.BackgroundJob
{
    /// <summary>
    /// پیام‌های همگانی موعدرسیده را پشت سر هم می‌فرستد. هر دور یک scope تازه؛ اگر کاری بود
    /// بلافاصله سراغ بعدی می‌رود، وگرنه ۱۵ ثانیه صبر می‌کند. ارسال نیمه‌کاره بعد از ری‌استارت
    /// ادامه پیدا می‌کند (BroadcastDeliveries جلوی ارسال دوباره را می‌گیرد).
    /// </summary>
    internal sealed class BroadcastDispatcherService(IServiceScopeFactory scopes, ILogger<BroadcastDispatcherService> logger)
        : BackgroundService
    {
        private static readonly TimeSpan Idle = TimeSpan.FromSeconds(15);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                var worked = false;
                try
                {
                    using var scope = scopes.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<IBroadcastSender>();
                    worked = await sender.RunDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Broadcast dispatcher loop failed");
                }

                if (!worked)
                {
                    try
                    {
                        await Task.Delay(Idle, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }
    }
}
