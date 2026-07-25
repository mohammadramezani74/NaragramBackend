using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.GetMessages
{
   
        public sealed record GroupMessagesQuery(Guid ConversationId, int count = 50) : IQuery<MessageResponse[]>;
    
}
