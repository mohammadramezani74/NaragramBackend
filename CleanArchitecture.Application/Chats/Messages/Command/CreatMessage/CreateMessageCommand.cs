using CleanArchitecture.Application.Common.Messaging;
namespace CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;

public sealed record CreateMessageCommand(Guid ConversationId,string Message,Guid?ParentId):ICommands<Guid>;

