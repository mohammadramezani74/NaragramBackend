using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;

namespace CleanArchitecture.Domain.Entities.Chat
{
    /// <summary>
    /// یک کار شخصی که کاربر برای خودش درباره‌ی یک مخاطب می‌نویسد.
    /// فقط صاحبش (OwnerId) آن را می‌بیند؛ مخاطب هیچ خبری از آن ندارد.
    /// </summary>
    public sealed class ContactTask : BaseEntity
    {
        public const int MaxTextLength = 500;
        public const int MaxPreviewLength = 200;

        private ContactTask() { }

        public Guid OwnerId { get; private set; }
        public User? Owner { get; private set; }

        public Guid ContactId { get; private set; }
        public User? Contact { get; private set; }

        public string Text { get; private set; } = null!;

        /// <summary>فقط تاریخ (بدون ساعت).</summary>
        public DateTime? DueDate { get; private set; }

        public bool IsDone { get; private set; }
        public DateTime? DoneAt { get; private set; }

        public int SortOrder { get; private set; }

        // پیامی که کار از روی آن ساخته شده. کلید خارجی عمداً گذاشته نشده تا
        // حذف یا پاک‌کردن تاریخچه‌ی چت به کار دست نزند؛ متن کوتاه پیام هم
        // نگه داشته می‌شود تا بعد از حذف پیام هم معلوم باشد کار از کجا آمده.
        public Guid? SourceMessageId { get; private set; }
        public string? SourcePreview { get; private set; }
        public DateTime? SourceSentAt { get; private set; }

        // یادآوری: زمان به UTC نگه داشته می‌شود تا منطقه‌ی زمانی سرور و مرورگر
        // روی آن اثری نداشته باشد. ReminderSentAtUtc یعنی اعلانش رفته است.
        public DateTime? RemindAtUtc { get; private set; }
        public DateTime? ReminderSentAtUtc { get; private set; }

        public static ContactTask Create(
            Guid ownerId,
            Guid contactId,
            string text,
            DateTime? dueDate,
            int sortOrder,
            Guid? id = null,
            DateTime? createdAt = null)
        {
            var task = new ContactTask
            {
                OwnerId = ownerId,
                ContactId = contactId,
                CreatedByUserId = ownerId,
                SortOrder = sortOrder,
            };

            if (id is Guid given && given != Guid.Empty)
                task.Id = given;

            if (createdAt is DateTime at)
                task.CreateDate = at;

            task.SetText(text);
            task.SetDueDate(dueDate);
            return task;
        }

        public void SetText(string text)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length > MaxTextLength)
                text = text[..MaxTextLength];

            Text = text;
            Touch();
        }

        public void SetDueDate(DateTime? dueDate)
        {
            DueDate = dueDate?.Date;
            Touch();
        }

        /// <summary>
        /// زمان یادآوری را عوض می‌کند. اگر واقعاً تغییر کرده باشد، وضعیت «ارسال شده»
        /// پاک می‌شود تا یادآوری جدید دوباره فرستاده شود.
        /// </summary>
        public void SetReminder(DateTime? remindAtUtc)
        {
            if (remindAtUtc is DateTime at)
                remindAtUtc = DateTime.SpecifyKind(new DateTime(at.Ticks - at.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);

            if (RemindAtUtc == remindAtUtc)
                return;

            RemindAtUtc = remindAtUtc;
            ReminderSentAtUtc = null;
            Touch();
        }

        public void SetDone(bool done)
        {
            if (IsDone == done)
                return;

            IsDone = done;
            DoneAt = done ? DateTime.Now : null;
            Touch();
        }

        public void SetSortOrder(int order)
        {
            SortOrder = order;
        }

        public void LinkToMessage(Guid messageId, string? preview, DateTime sentAt)
        {
            preview = (preview ?? string.Empty).Trim();
            if (preview.Length > MaxPreviewLength)
                preview = preview[..MaxPreviewLength];

            SourceMessageId = messageId;
            SourcePreview = preview;
            SourceSentAt = sentAt;
        }

        private void Touch()
        {
            ModifiedDate = DateTime.Now;
            ModifiedById = OwnerId;
        }
    }
}
