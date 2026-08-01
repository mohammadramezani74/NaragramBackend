using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Command.PinMessage
{
    public sealed record TogglePinMessageCommand(Guid MessageId, bool Pin) : ICommands;
}
