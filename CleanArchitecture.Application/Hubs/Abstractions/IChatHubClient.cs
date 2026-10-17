using CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Application.Users.Queries.GetUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Hubs.Abstractions
{
    public interface IChatHubClient
    {
        Task UserConnected(UserDto user);
        Task OnlineUserList(IEnumerable<UserDto> users);
        Task UserIsOnline(Guid UserId);
        Task UserIsOffline(Guid UserId);
        Task MessagedReceived(ChatMessageDto message);
        Task EditedMessageReceived(EditedMessageDto message);
        Task DeletedMessageReceived(Guid MessageId);

        /// <summary>
        /// آخرین پیام گفتگو عوض شد (حذف پیام یا پاک کردن تاریخچه).
        /// متد جدا اضافه شد و امضای DeletedMessageReceived دست نخورد، تا
        /// کلاینت‌های کش‌شده که این را نمی‌شناسند نشکنند و صرفاً نادیده بگیرند.
        /// </summary>
        Task LastMessageChanged(LastMessageChangedDto Message);
        Task MessagedSeenReceived(List<Guid> MessageId);
        Task IncreaseMessageCount(Guid UserId);
        Task ReceivedNotifications(NotificationModelDto notify);
        Task SetLastSeenUser(LastSeenModelDto lastSeenModel);
        Task ReceivedReactions(TypingReactionDto MessageType);
        Task BlockUser(BlockDto blockDto);
        Task ReceivedEmojiReact(MessageReactionDto reaction);
        Task GetMissedMessages(List<ChatMessageDto> messages);
        Task GetDeletedChannel(Guid channelId);
        Task ReceiveNewChannel(GetUserResponse user);
        Task ChatHistoryCleared(Guid conversationOrChannelId);
        Task MessagePinChanged(Guid scopeId, Guid messageId, bool pinned);

        /// <summary>
        /// کارهای شخصیِ کاربر درباره‌ی یک مخاطب عوض شد. فقط به دستگاه‌های خودِ
        /// صاحب کارها فرستاده می‌شود تا تب‌ها و گوشی‌اش هم‌گام بمانند.
        /// </summary>
        Task ContactTasksChanged(Guid contactId);

        /// <summary>زمان یادآوری یکی از کارهای کاربر رسید.</summary>
        Task TaskReminder(CleanArchitecture.Application.ContactTasks.TaskReminderDto reminder);

        /// <summary>
        /// ربات یکی از پیام‌های خودش را عوض کرد (متن یا دکمه‌ها؛ مثلاً رفتن به زیرمنو).
        /// جدا از EditedMessageReceived است تا کلاینت‌های قدیمی که دکمه نمی‌شناسند نشکنند.
        /// </summary>
        Task BotMessageUpdated(ChatMessageDto message);

        /// <summary>کیبورد ربات یا وضعیت «شروع» برای این کاربر عوض شد.</summary>
        Task BotStateChanged(BotStateDto state);

        /// <summary>حساب غیرفعال شد یا از همه‌ی دستگاه‌ها خارج شد؛ برنامه باید خارج شود. پیام برای نمایش است.</summary>
        Task ForceLogout(string message);
    }
}
