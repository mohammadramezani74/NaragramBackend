using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.FileMessages.Query
{
    public sealed record GetTargetFileCommand(Guid FileId) : IQuery<GetFileResponse>;
}
