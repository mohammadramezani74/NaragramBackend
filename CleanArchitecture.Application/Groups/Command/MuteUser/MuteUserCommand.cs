using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Command.MuteUser
{
    public sealed record MuteUserCommand(Guid GroupId,Guid UserId,bool isMute):ICommands;
}
