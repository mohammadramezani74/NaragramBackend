using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CleanArchitecture.Application.Channels.Command.ChannelMessage
{
    public sealed record SendChannelMessageCommand(Guid ChannelId, string Message, Guid? ParentId) :ICommands<Guid>;
}
