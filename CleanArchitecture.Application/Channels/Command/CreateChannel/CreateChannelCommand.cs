using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.CreateChannel
{
    public sealed record CreateChannelCommand(string Title,
        string Description,
        string UserName,
        bool IsPublic
        ):ICommands;
 
}
