using CleanArchitecture.Application.Bots.Hr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.BackgroundJob
{
    /// <summary>
    /// خلاصه‌ی صبحگاهی کارتابل ربات منابع انسانی. مثل خلاصه‌ی دستیار: هر دقیقه نگاه می‌کند و در بازه‌ی
    /// ارسال دسته‌دسته می‌فرستد (هر دسته یک scope تازه). جدا از دستیار است تا خطای یکی دیگری را نخواباند.
    /// </summary>
    internal sealed class KartableDigestDispatcherService(IServiceScopeFactory scopes, ILogger<KartableDigestDispatcherService> logger)
        : BackgroundService
    {
        private static readonly TimeSpan Idle = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan BetweenBatches = TimeSpan.FromSeconds(2);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            while (!stoppingToken.IsCancellationRequested)
            {
                var worked = false;
                try
                {
                    using var scope = scopes.CreateScope();
                    var digest = scope.ServiceProvider.GetRequiredService<IKartableDigestService>();
                    worked = await digest.RunDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Kartable digest loop failed");
                }

                try
                {
                    await Task.Delay(worked ? BetweenBatches : Idle, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
