using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities.Admin
{
    public enum BroadcastStatus
    {
        Scheduled = 0,
        Sending = 1,
        Sent = 2,
        Cancelled = 3,
        Failed = 4,
    }

    /// <summary>
    /// پیام همگانی از طرف ربات سیستمی «اطلاع‌رسانی ناراگرام». یک‌طرفه است؛ گیرنده‌ها از روی
    /// AudienceJson در لحظه‌ی ارسال حساب می‌شوند تا کاربرهای تازه یا غیرفعال‌شده درست دیده شوند.
    /// </summary>
    public sealed class Broadcast : BaseEntity
    {
        public const int MaxTitleLength = 120;
        public const int MaxTextLength = 4000;
        public const int MaxButtonTextLength = 40;
        public const int MaxUrlLength = 500;

        private Broadcast() { }

        /// <summary>عنوان داخلی برای تاریخچه؛ به گیرنده نشان داده نمی‌شود.</summary>
        public string Title { get; private set; } = string.Empty;
        public string Text { get; private set; } = string.Empty;

        /// <summary>info | urgent | reminder</summary>
        public string Kind { get; private set; } = "info";
        public string? ButtonText { get; private set; }
        public string? ButtonUrl { get; private set; }
        public string AudienceJson { get; private set; } = "{}";
        public string AudienceLabel { get; private set; } = string.Empty;

        public DateTime ScheduledAt { get; private set; }
        public BroadcastStatus Status { get; private set; }
        public DateTime? StartedAt { get; private set; }
        public DateTime? FinishedAt { get; private set; }
        public int Total { get; private set; }
        public int Delivered { get; private set; }
        public string? Error { get; private set; }
        public string CreatedByName { get; private set; } = string.Empty;

        public static Broadcast Create(
            string title, string text, string kind, string? buttonText, string? buttonUrl,
            string audienceJson, string audienceLabel, DateTime scheduledAt, Guid createdBy, string createdByName)
            => new()
            {
                Title = title,
                Text = text,
                Kind = kind,
                ButtonText = buttonText,
                ButtonUrl = buttonUrl,
                AudienceJson = audienceJson,
                AudienceLabel = audienceLabel,
                ScheduledAt = scheduledAt,
                Status = BroadcastStatus.Scheduled,
                CreatedByUserId = createdBy,
                CreatedByName = createdByName,
            };

        public bool Cancel()
        {
            if (Status != BroadcastStatus.Scheduled) return false;
            Status = BroadcastStatus.Cancelled;
            FinishedAt = DateTime.Now;
            return true;
        }

        public void Start(int total)
        {
            Status = BroadcastStatus.Sending;
            StartedAt ??= DateTime.Now;
            Total = total;
        }

        public void Progress(int delivered) => Delivered = delivered;

        public void Finish()
        {
            Status = BroadcastStatus.Sent;
            FinishedAt = DateTime.Now;
        }

        public void Fail(string error)
        {
            Status = BroadcastStatus.Failed;
            FinishedAt = DateTime.Now;
            Error = error.Length > 500 ? error[..500] : error;
        }
    }

    /// <summary>یک گیرنده‌ی پیام همگانی؛ برای جلوگیری از ارسال دوباره و شمارش «دیده‌شده».</summary>
    public sealed class BroadcastDelivery
    {
        private BroadcastDelivery() { }

        public Guid Id { get; private set; }
        public Guid BroadcastId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid MessageId { get; private set; }
        public DateTime At { get; private set; }

        public static BroadcastDelivery Create(Guid broadcastId, Guid userId, Guid messageId)
            => new() { Id = Guid.NewGuid(), BroadcastId = broadcastId, UserId = userId, MessageId = messageId, At = DateTime.Now };
    }
}
