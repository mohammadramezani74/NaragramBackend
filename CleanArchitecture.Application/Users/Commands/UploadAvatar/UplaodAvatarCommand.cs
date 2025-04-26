using CleanArchitecture.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.UploadAvatar
{
    public sealed record UplaodAvatarCommand(IFormFile file):ICommands<string?>;
}
