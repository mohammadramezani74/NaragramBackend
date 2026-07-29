using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages
{
    public sealed record MessagesQuery(
        Guid ConversationId,
        DateTime? Before = null,
        int Take = 30) : IQuery<MessagesPage>;

}
