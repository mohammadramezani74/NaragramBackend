using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.ContactTasks;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CleanArchitecture.Infrastructure.BackgroundJob
{
    /// <summary>
    /// یادآوری‌های رسیده‌ی کارهای شخصی را می‌فرستد: رویداد SignalR برای صفحه‌های
    /// باز صاحب کار، و اعلان Firebase برای وقتی که صفحه بسته است.
    /// </summary>
    [DisallowConcurrentExecution]
    internal sealed class SendContactTaskRemindersJob(
        IApplicationUnitOfWork uow,
        INotificationService notifications,
        IHubContext<NaraHub, IChatHubClient> hub,
        ILogger<SendContactTaskRemindersJob> logger) : IJob
    {
        private const int BatchSize = 100;

        /// <summary>
        /// اگر سرور مدتی خاموش بوده، یادآوری خیلی قدیمی دیگر فرستاده نمی‌شود
        /// (فقط علامت «ارسال شده» می‌خورد) تا کاربر با سیلی از اعلان روبه‌رو نشود.
        /// </summary>
        private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(12);

        public async Task Execute(IJobExecutionContext context)
        {
            var ct = context.CancellationToken;

            try
            {
                var now = DateTime.UtcNow;

                var due = await uow.ContactTasks.AsNoTracking()
                    .Where(t => t.RemindAtUtc != null
                                && t.RemindAtUtc <= now
                                && t.ReminderSentAtUtc == null
                                && !t.IsDone)
                    .OrderBy(t => t.RemindAtUtc)
                    .Take(BatchSize)
                    .Select(t => new
                    {
                        t.Id,
                        t.OwnerId,
                        t.ContactId,
                        t.Text,
                        t.DueDate,
                        RemindAtUtc = t.RemindAtUtc!.Value,
                        FirstName = t.Contact!.FirsName,
                        t.Contact.LastName,
                        t.Contact.UserName,
                    })
                    .ToListAsync(ct);

                foreach (var r in due)
                {
                    // «برداشتن» اتمی: اگر نمونه‌ی دیگری از برنامه (مثلاً هنگام
                    // recycle در IIS) زودتر برداشته باشد، اینجا صفر برمی‌گردد.
                    var claimed = await uow.ContactTasks
                        .Where(t => t.Id == r.Id && t.ReminderSentAtUtc == null)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReminderSentAtUtc, (DateTime?)now), ct);

                    if (claimed == 0 || now - r.RemindAtUtc > StaleAfter)
                        continue;

                    var contactName = $"{r.FirstName} {r.LastName}".Trim();
                    if (contactName.Length == 0)
                        contactName = r.UserName ?? "مخاطب";

                    var reminder = new TaskReminderDto
                    {
                        TaskId = r.Id,
                        ContactId = r.ContactId,
                        ContactName = contactName,
                        Text = r.Text,
                        DueDate = r.DueDate,
                        RemindAt = new DateTimeOffset(DateTime.SpecifyKind(r.RemindAtUtc, DateTimeKind.Utc)),
                    };

                    try
                    {
                        await hub.Clients.User(r.OwnerId.ToString()).TaskReminder(reminder);
                        await hub.Clients.User(r.OwnerId.ToString()).ContactTasksChanged(r.ContactId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Task reminder {TaskId}: SignalR send failed", r.Id);
                    }

                    var tokens = await uow.FireBaseTokens.AsNoTracking()
                        .Where(x => x.UserId == r.OwnerId)
                        .Select(x => x.Token)
                        .ToListAsync(ct);

                    foreach (var token in tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
                    {
                        await notifications.SendReminder(
                            token,
                            $"یادآوری · {contactName}",
                            r.Text,
                            $"task-{r.Id}");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sending contact task reminders failed");
            }
        }
    }
}
