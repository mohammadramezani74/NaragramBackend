using CleanArchitecture.Application.Common.Messaging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Command.UploadGroupAvtar
{
    public sealed record UplaodGroupAvatarCommand(IFormFile file,Guid GroupId) : ICommands<string?>;
}
