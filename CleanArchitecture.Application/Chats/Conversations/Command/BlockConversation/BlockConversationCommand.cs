using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Command.BlockConversation
{
    public sealed record BlockConversationCommand(bool IsBlock, Guid ConversationId)
        : ICommands<bool>;
}
