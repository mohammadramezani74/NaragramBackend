

using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Chats.Conversations.Command.PinConversation;

public sealed   record PinConversationCommand(bool IsPin,Guid ConversationId)
    :ICommands;

