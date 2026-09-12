using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages
{
    /// <summary>
    /// آخرین پیام هر گفتگو در دو جا نگه داشته می‌شود: یک اسنپ‌شات روی خود گفتگو
    /// یا کانال، و محاسبه از روی جدول پیام‌ها. حذف پیام تا الان فقط جدول پیام‌ها
    /// را عوض می‌کرد و اسنپ‌شات دست‌نخورده می‌ماند، پس متن پیام پاک‌شده در لیست
    /// مکالمات باقی می‌ماند.
    ///
    /// این کلاس اسنپ‌شات را با واقعیت هماهنگ می‌کند و همان مقدار را برمی‌گرداند
    /// تا بشود زنده به کلاینت‌ها اطلاع داد.
    /// </summary>
    public sealed record LastMessageSnapshot(
        Guid ScopeId,
        Guid? MessageId,
        string Text,
        DateTime? SentAt);

    public static class LastMessageSync
    {
        /// <summary>
        /// اسنپ‌شات گفتگو را از روی آخرین پیام باقی‌مانده بازسازی می‌کند.
        /// اگر پیامی نمانده باشد متن خالی می‌شود ولی گفتگو در لیست می‌ماند.
        /// </summary>
        public static async Task<LastMessageSnapshot> SyncConversationAsync(
            IApplicationUnitOfWork uow, Guid conversationId, CancellationToken ct)
        {
            var conversation = await uow.Conversation
                .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

            var last = await uow.Messages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreateDate)
                .Select(m => new { m.Id, m.Content, m.MessageType, m.CreateDate, m.CreatedByUserId })
                .FirstOrDefaultAsync(ct);

            var text = last is null ? string.Empty : Preview(last.Content, last.MessageType);

            if (conversation is not null)
            {
                conversation.LastMessageId = last?.Id;
                conversation.LastMessageText = text;
                conversation.LastMessageSentAt = last?.CreateDate;
                conversation.LastUserSenderMessageId = last?.CreatedByUserId;
            }

            return new LastMessageSnapshot(conversationId, last?.Id, text, last?.CreateDate);
        }

        /// <summary>همان کار برای کانال.</summary>
        public static async Task<LastMessageSnapshot> SyncChannelAsync(
            IApplicationUnitOfWork uow, Guid channelId, CancellationToken ct)
        {
            var channel = await uow.Channels
                .FirstOrDefaultAsync(c => c.Id == channelId, ct);

            var last = await uow.Messages
                .AsNoTracking()
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.CreateDate)
                .Select(m => new { m.Id, m.Content, m.MessageType, m.CreateDate, m.CreatedByUserId })
                .FirstOrDefaultAsync(ct);

            var text = last is null ? string.Empty : Preview(last.Content, last.MessageType);

            if (channel is not null)
            {
                channel.LastMessageId = last?.Id;
                channel.LastMessageText = text;
                channel.LastMessageSentAt = last?.CreateDate;
                channel.LastUserSenderMessageId = last?.CreatedByUserId;
            }

            return new LastMessageSnapshot(channelId, last?.Id, text, last?.CreateDate);
        }

        /// <summary>
        /// متن کوتاه لیست مکالمات. برای پیام متنی خود متن و برای بقیه نوعش.
        /// حالت Text هم پوشش داده شده — نسخه‌های مشابه در جاهای دیگر switch
        /// ناقص دارند و برای پیام متنی استثنا می‌دهند.
        /// </summary>
        private static string Preview(string? content, MessageType type)
        {
            var text = type switch
            {
                MessageType.Video => "پیام ویدیویی",
                MessageType.Audio => "پیام صوتی",
                MessageType.Image => "پیام تصویری",
                MessageType.Document => "پیام  اسنادی",
                MessageType.Location => "لوکیشن",
                _ => content ?? string.Empty
            };

            return text.Length > 30 ? text.Substring(0, 30) + "..." : text;
        }
    }
}
