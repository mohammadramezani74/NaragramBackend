using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Command.CreateGroupConverSation
{
    public sealed record  CreateGroupConversationCommand(List<Guid> Others,string Title,string UserName,string? Description):ICommands;
   
}
