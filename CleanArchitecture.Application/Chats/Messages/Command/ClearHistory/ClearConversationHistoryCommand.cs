using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Command.ClearHistory
{

    public sealed record ClearConversationHistoryCommand(Guid ConversationId) : ICommands;
}
