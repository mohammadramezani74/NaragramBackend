using CleanArchitecture.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.FileMessages.Query
{
    public sealed record GetFileResponse(byte[] FileData, byte[]?thumbnail,string Name,MessageType Type);
}
