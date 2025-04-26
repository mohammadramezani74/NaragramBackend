using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage
{
    public sealed record class DeleteMessageCommand(Guid MessageId,Guid OtherUserId):ICommands;
}
