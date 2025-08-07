using CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Hubs.Models;
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
        Task MessagedSeenReceived(List<Guid> MessageId);
        Task IncreaseMessageCount(Guid UserId);
        Task ReceivedNotifications(NotificationModelDto notify);
        Task SetLastSeenUser(LastSeenModelDto lastSeenModel);
        Task ReceivedReactions(TypingReactionDto MessageType);
        Task BlockUser(BlockDto blockDto);
        Task ReceivedEmojiReact(MessageReactionDto reaction);
        Task GetMissedMessages(List<ChatMessageDto> messages);
    }
}
