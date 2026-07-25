using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Command.RemoveMemberFromGroup
{
    public sealed record RemoveMemberFromGroupCommand(Guid ConversationId,Guid memberId):ICommands;
}
