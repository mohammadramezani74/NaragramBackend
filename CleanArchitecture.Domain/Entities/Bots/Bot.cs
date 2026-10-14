using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities.Bots
{
    /// <summary>
    /// تنظیمات یک ربات. خودِ ربات یک کاربر است (User با IsBot = true) تا همه‌ی
    /// چیزهای موجود — گفتگوی خصوصی، پیام، اعلان، لیست چت‌ها — بدون تغییر برایش کار کنند.
    /// این جدول فقط چیزهایی را نگه می‌دارد که کاربر عادی ندارد.
    ///
    /// منطق ربات اینجا نیست: منو در BotMenuNode و جریان‌ها در کد (IBotFlow).
    /// </summary>
    public sealed class Bot : BaseEntity
    {
        public const int MaxHandleLength = 32;
        public const int MaxDescriptionLength = 500;

        private Bot() { }

        public Guid UserId { get; private set; }
        public User? User { get; private set; }

        /// <summary>شناسه‌ی یکتای ربات، مثل hr_bot. بعد از ساخت عوض نمی‌شود.</summary>
        public string Handle { get; private set; } = null!;

        /// <summary>در صفحه‌ی «شروع» و پروفایل ربات نمایش داده می‌شود.</summary>
        public string? Description { get; private set; }

        public bool IsEnabled { get; private set; }

        public BotAccessMode AccessMode { get; private set; }

        /// <summary>
        /// آرایه‌ی JSON از شناسه‌ها: شناسه‌ی نقش‌ها وقتی AccessMode = Roles و
        /// شناسه‌ی کاربرها وقتی AccessMode = Users. برای All خالی است.
        /// ربات‌ها کم‌اند و این فهرست همیشه کامل در حافظه خوانده می‌شود،
        /// پس جدول جدا لازم نبود.
        /// </summary>
        public string AccessListJson { get; private set; } = "[]";

        /// <summary>
        /// دستورهای منوی «/»، آرایه‌ی JSON از {command, description, nodeId}.
        /// هر دستور به یک گره منو اشاره می‌کند. /شروع، /راهنما و /انصراف همیشه
        /// هستند و اینجا نوشته نمی‌شوند.
        /// </summary>
        public string CommandsJson { get; private set; } = "[]";

        public static Bot Create(Guid userId, string handle, string? description, Guid createdBy)
        {
            var bot = new Bot
            {
                UserId = userId,
                Handle = handle,
                IsEnabled = false,
                AccessMode = BotAccessMode.Users,
                CreatedByUserId = createdBy,
            };
            bot.SetDescription(description);
            return bot;
        }

        public void SetDescription(string? description)
        {
            description = description?.Trim();
            if (description is { Length: > MaxDescriptionLength })
                description = description[..MaxDescriptionLength];

            Description = string.IsNullOrEmpty(description) ? null : description;
            Touch();
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            Touch();
        }

        public void SetAccess(BotAccessMode mode, string accessListJson)
        {
            AccessMode = mode;
            AccessListJson = mode == BotAccessMode.All ? "[]" : accessListJson;
            Touch();
        }

        public void SetCommands(string commandsJson)
        {
            CommandsJson = string.IsNullOrWhiteSpace(commandsJson) ? "[]" : commandsJson;
            Touch();
        }

        private void Touch() => ModifiedDate = DateTime.Now;
    }
}
