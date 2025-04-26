using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Hubs.Models;

namespace CleanArchitecture.Application.Hubs.Abstractions
{
    public interface IChatHubServer
    {
        Task SetUserOnline(UserDto user);
        Task SetUserOffline(UserDto user);
        Task MessageSeen(MessageSeenDto messagesForSeen);
        Task TypingReaction(TypingReactionDto reaction);
    }
}
