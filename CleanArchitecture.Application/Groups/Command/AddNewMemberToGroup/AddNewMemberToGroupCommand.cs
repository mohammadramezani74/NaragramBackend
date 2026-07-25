using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CleanArchitecture.Application.Groups.Command.AddNewMemberToGroup
{
    public record AddNewMemberToGroupCommand(Guid ConversationId,Guid NewUserId):ICommands;
}
