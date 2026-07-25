using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Command.SendFileMessage
{
    public sealed record  SendFileMessageCommand(Guid ConversationId,

        string? caption,
        IFormFile file
        ) : ICommands<CreateFileMessageResponse>;
}
