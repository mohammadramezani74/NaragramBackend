using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.Members.AddMember
{
    public sealed record AddNewMemberCommand(Guid ChannelId,Guid MemberId):ICommands;
}
