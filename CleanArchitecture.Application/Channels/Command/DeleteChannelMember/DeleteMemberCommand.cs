using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.DeleteChannelMember
{
    public sealed record DeleteMemberCommand(Guid memberid,Guid channelid):ICommands;

}
