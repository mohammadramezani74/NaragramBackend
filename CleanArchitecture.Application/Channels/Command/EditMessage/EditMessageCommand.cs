using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.EditMessage
{
    public sealed record  EditMessageCommand(Guid MessageId, string Message, Guid ChannelId) : ICommands;
}
