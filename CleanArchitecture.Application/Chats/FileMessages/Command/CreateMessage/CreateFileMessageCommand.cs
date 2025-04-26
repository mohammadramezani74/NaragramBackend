using CleanArchitecture.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage
{
    public sealed record CreateFileMessageCommand(Guid ConversationId,

        string? caption,
        IFormFile file
        ):ICommands<CreateFileMessageResponse>;
}
