using System.Linq.Expressions;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Entities.Chat;

namespace CleanArchitecture.Application.ContactTasks
{
    public sealed class ContactTaskDto
    {
        public Guid Id { get; set; }
        public Guid ContactId { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public bool IsDone { get; set; }
        public DateTime? DoneAt { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid? SourceMessageId { get; set; }
        public string? SourcePreview { get; set; }
        public DateTime? SourceSentAt { get; set; }

        /// <summary>با آفست فرستاده می‌شود تا مرورگر به ساعت محلی خودش تبدیل کند.</summary>
        public DateTimeOffset? RemindAt { get; set; }

        /// <summary>یادآوری فرستاده شده است.</summary>
        public bool ReminderSent { get; set; }

        public static readonly Expression<Func<ContactTask, ContactTaskDto>> Projection = t => new ContactTaskDto
        {
            Id = t.Id,
            ContactId = t.ContactId,
            Text = t.Text,
            DueDate = t.DueDate,
            IsDone = t.IsDone,
            DoneAt = t.DoneAt,
            SortOrder = t.SortOrder,
            CreatedAt = t.CreateDate,
            SourceMessageId = t.SourceMessageId,
            SourcePreview = t.SourcePreview,
            SourceSentAt = t.SourceSentAt,
            RemindAt = t.RemindAtUtc == null
                ? (DateTimeOffset?)null
                : new DateTimeOffset(DateTime.SpecifyKind(t.RemindAtUtc.Value, DateTimeKind.Utc)),
            ReminderSent = t.ReminderSentAtUtc != null,
        };

        private static readonly Func<ContactTask, ContactTaskDto> Compiled = Projection.Compile();

        public static ContactTaskDto From(ContactTask task) => Compiled(task);
    }

    public sealed class ContactTaskListDto
    {
        public Guid ContactId { get; set; }
        public List<ContactTaskDto> Open { get; set; } = new();

        /// <summary>آخرین انجام‌شده‌ها (حداکثر <see cref="ContactTaskRules.DoneListLimit"/> مورد).</summary>
        public List<ContactTaskDto> Done { get; set; } = new();

        /// <summary>تعداد کل انجام‌شده‌ها، حتی اگر همه در Done نیامده باشند.</summary>
        public int DoneCount { get; set; }
    }

    public sealed class ContactTaskCountDto
    {
        public Guid ContactId { get; set; }
        public int OpenCount { get; set; }
        public int OverdueCount { get; set; }
    }

    /// <summary>رویداد SignalR وقتی زمان یادآوری یک کار می‌رسد.</summary>
    public sealed class TaskReminderDto
    {
        public Guid TaskId { get; set; }
        public Guid ContactId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTimeOffset RemindAt { get; set; }
    }

    internal static class ContactTaskRules
    {
        /// <summary>
        /// یادآوری کمی قبل از «الان» هم پذیرفته می‌شود (ساعت مرورگر و سرور
        /// چند ثانیه اختلاف دارند)، ولی زمانِ واقعاً گذشته رد می‌شود.
        /// </summary>
        public static readonly TimeSpan ReminderPastTolerance = TimeSpan.FromMinutes(2);

        public static OperationResult? ValidateReminder(DateTimeOffset? remindAt)
        {
            if (remindAt is null)
                return null;

            if (remindAt.Value.UtcDateTime < DateTime.UtcNow - ReminderPastTolerance)
                return new OperationResult().BadRequest("زمان یادآوری گذشته است.");

            if (remindAt.Value.UtcDateTime > DateTime.UtcNow.AddYears(2))
                return new OperationResult().BadRequest("زمان یادآوری خیلی دور است.");

            return null;
        }

        /// <summary>سقف کارهای یک کاربر درباره‌ی یک مخاطب؛ فقط جلوی سوءاستفاده را می‌گیرد.</summary>
        public const int MaxTasksPerContact = 1000;

        public const int DoneListLimit = 100;

        public const int MaxImportItems = 500;
    }
}
