using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Query.PinnedMessages
{
    public sealed record PinnedMessagesQuery(Guid ScopeId) : IQuery<PinnedMessageDto[]>;
}
