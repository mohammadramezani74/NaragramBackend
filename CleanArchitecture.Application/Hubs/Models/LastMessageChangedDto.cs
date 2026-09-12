using System;

namespace CleanArchitecture.Application.Hubs.Models
{
    /// <summary>
    /// وقتی آخرین پیام یک گفتگو عوض می‌شود — مثلاً با حذف پیام یا پاک کردن
    /// تاریخچه — این به کلاینت‌ها فرستاده می‌شود تا لیست مکالمات بدون رفرش
    /// به‌روز شود.
    ///
    /// ScopeId همان کلیدی است که پیام‌ها هم با آن مسیریابی می‌شوند:
    /// شناسه‌ی گفتگو برای خصوصی و گروه، شناسه‌ی کانال برای کانال.
    /// </summary>
    public sealed class LastMessageChangedDto
    {
        public Guid ScopeId { get; set; }

        /// <summary>شناسه‌ی آخرین پیام باقی‌مانده؛ اگر پیامی نمانده null است.</summary>
        public Guid? MessageId { get; set; }

        /// <summary>متن کوتاه برای لیست مکالمات؛ اگر پیامی نمانده خالی است.</summary>
        public string Text { get; set; } = string.Empty;

        public DateTime? SentAt { get; set; }
    }
}
