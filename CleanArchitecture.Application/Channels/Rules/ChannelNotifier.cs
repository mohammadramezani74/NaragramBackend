using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Channels.Rules
{
    /// <summary>
    /// اعلان پست جدید کانال، یک‌جا برای پیام متنی و فایل.
    /// </summary>
    public static class ChannelNotifier
    {
        private const string ChatUrl = "https://naragram.irannara.com/chat";
        private const int MaxBodyLength = 150;

        /// <summary>
        /// به همه‌ی اعضا جز فرستنده: اعلان Firebase (برای صفحه‌ی بسته) و اعلان
        /// مرورگر از طریق هاب (برای صفحه‌ی باز)، مثل چت خصوصی و گروه.
        /// </summary>
        public static async Task NotifyNewPostAsync(
            IApplicationUnitOfWork uow,
            INotificationService notifications,
            IHubContext<NaraHub, IChatHubClient> hub,
            Guid channelId,
            string channelTitle,
            Guid senderId,
            string body,
            string? avatarUrl,
            CancellationToken ct)
        {
            body = Shorten(body);

            try
            {
                var tokens = await uow.FireBaseTokens.AsNoTracking()
                    .Where(t => t.UserId != senderId
                                && uow.ChannelMembers.Any(m => m.ChannelId == channelId && m.UserId == t.UserId))
                    .Select(t => t.Token)
                    .Distinct()
                    .ToListAsync(ct);

                // در پس‌زمینه: کانال ممکن است صدها عضو داشته باشد و فرستنده نباید
                // تا تمام شدن ارسال‌ها منتظر بماند. SendToMany خودش خطا را می‌خورد.
                if (tokens.Count > 0)
                    _ = notifications.SendToMany(tokens, channelTitle, body);
            }
            catch (Exception)
            {
                // اعلان نباید ارسال پست را خراب کند.
            }

            try
            {
                var notification = new NotificationModelDto(channelTitle, avatarUrl ?? string.Empty, body, ChatUrl);

                await hub.Clients
                    .GroupExcept(channelId.ToString(), NaraHub.GetUserConnections(senderId))
                    .ReceivedNotifications(notification);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>متن اعلان برای پست فایل: کپشن اگر هست، وگرنه نوع فایل.</summary>
        public static string FileBody(string? caption, MessageType type)
        {
            if (!string.IsNullOrWhiteSpace(caption))
                return caption;

            return type switch
            {
                MessageType.Image => "🖼 تصویر",
                MessageType.Video => "🎬 ویدیو",
                MessageType.Audio => "🎤 پیام صوتی",
                MessageType.Location => "📍 موقعیت مکانی",
                _ => "📎 فایل",
            };
        }

        private static string Shorten(string? text)
        {
            text = (text ?? string.Empty).Trim();
            return text.Length > MaxBodyLength ? text[..MaxBodyLength] + "…" : text;
        }
    }
}
