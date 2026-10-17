using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Channels.Rules
{
    /// <summary>
    /// قوانین دسترسی کانال که در چند هندلر لازم است.
    /// </summary>
    public static class ChannelPermissions
    {
        public const string CannotPostMessage = "فقط سازنده و مدیران کانال می‌توانند در آن پیام بفرستند.";

        /// <summary>
        /// فقط سازنده‌ی کانال و مدیرانش اجازه‌ی ارسال پیام (متن، فایل یا فوروارد) دارند.
        /// قبلاً این فقط در رابط کاربری رعایت می‌شد و سرور هر عضوی را می‌پذیرفت.
        /// </summary>
        public static Task<bool> CanPostAsync(
            IApplicationUnitOfWork uow, Guid channelId, Guid userId, CancellationToken ct = default)
            => uow.Channels.AsNoTracking().AnyAsync(
                c => c.Id == channelId
                     && c.ArchivedAt == null // کانال بایگانی‌شده فقط‌خواندنی است
                     && (c.CreatedByUserId == userId || c.Admins.Any(a => a.UserId == userId)),
                ct);
    }
}
