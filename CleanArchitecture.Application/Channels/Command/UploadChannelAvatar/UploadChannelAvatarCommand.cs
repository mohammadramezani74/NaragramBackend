using CleanArchitecture.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.UploadChannelAvatar
{
    public sealed record UploadChannelAvatarCommand(IFormFile file,Guid channelId) : ICommands<string?>;

}
