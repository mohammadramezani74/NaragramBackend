using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Command.SendMessage
{
    public sealed record SendMeesageToGroupCommand(Guid ConversationId, string Message, Guid? ParentId, float? latitude, float? Longitude) : ICommands<Guid>;

}
