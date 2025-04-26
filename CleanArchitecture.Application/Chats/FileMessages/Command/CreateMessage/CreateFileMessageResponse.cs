using CleanArchitecture.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage
{
    public sealed class CreateFileMessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid FileId { get; set; }
        public MessageType MessageType { get; set; }
    }
}
