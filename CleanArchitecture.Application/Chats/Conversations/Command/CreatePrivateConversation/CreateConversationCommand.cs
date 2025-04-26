using CleanArchitecture.Application.Chats.Conversations.Query;
using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Chats.Conversations.Command.CreatePrivateConversation;

public sealed record CreateConversationCommand(Guid ToUserId) : ICommands<ConverSationResponse>;

