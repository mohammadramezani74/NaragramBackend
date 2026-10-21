using CleanArchitecture.Application.Bots.Assistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.BackgroundJob
{
    /// <summary>
    /// خلاصه‌ی صبحگاهی ربات دستیار. هر دقیقه نگاه می‌کند؛ در بازه‌ی ارسال، دسته‌دسته می‌فرستد
    /// (هر دسته یک scope تازه) تا همه‌ی مشترک‌های امروز برسند.
    /// </summary>
    internal sealed class AssistantDigestDispatcherService(IServiceScopeFactory scopes, ILogger<AssistantDigestDispatcherService> logger)
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
                    var digest = scope.ServiceProvider.GetRequiredService<IAssistantDigestService>();
                    worked = await digest.RunDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Assistant digest loop failed");
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
