using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Chats.Messages.Command.ClearHistory
{
    public sealed record ClearChannelHistoryCommand(Guid ChannelId) : ICommands;
}
