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
    }
}
