using System;

namespace CleanArchitecture.Domain.Entities.Chat
{
    /// <summary>
    /// جدول واسط بین پیام و فایل.
    ///
    /// قبلاً MessageId مستقیم روی ChatFiles بود، یعنی هر فایل دقیقاً به یک پیام
    /// تعلق داشت. برای فوروارد این کافی نبود: پیام جدید یا باید کل بایت‌های فایل
    /// را کپی می‌کرد (یک ویدیوی ۵۰۰ مگی در هر فوروارد ۵۰۰ مگ به دیتابیس اضافه
    /// می‌کرد) یا باید چند پیام به یک فایل اشاره می‌کردند. راه دوم انتخاب شد.
    ///
    /// هر دو کلید خارجی Restrict هستند: حذف پیام یا فایل بدون پاک کردن ردیف
    /// واسط با خطا می‌خورد. این عمدی است — بهتر است حذف با خطا شکست بخورد تا
    /// اینکه بی‌صدا فایلی را که هنوز جای دیگری استفاده می‌شود از بین ببرد.
    /// </summary>
    public sealed class MessageFile
    {
        public Guid MessageId { get; set; }
        public Message Message { get; set; } = null!;

        public Guid ChatFileId { get; set; }
        public ChatFiles ChatFile { get; set; } = null!;

        public DateTime CreateDate { get; set; } = DateTime.Now;

        public static MessageFile Link(Guid messageId, Guid chatFileId) => new()
        {
            MessageId = messageId,
            ChatFileId = chatFileId,
            CreateDate = DateTime.Now
        };
    }
}
