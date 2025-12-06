using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.ProcessFilesChannel
{
    public sealed record ProcessFilesChannelCommand(
        string? caption, IFormFile file, Guid ChannelId):ICommands<CreateFileMessageResponse>;
}
