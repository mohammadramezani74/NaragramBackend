using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public class ConversationAvatar:BaseEntity
    {
        public Guid Id { get; set; }
        public byte[]? FileData { get; private set; }
        public byte[]? Thumbnail { get; private set; }
        public string? FileName { get; private set; }
        public string? Extension { get; private set; }
        public decimal FileSize { get; private set; }
        public Conversation? Conversation { get; set; }
        public Guid ConversationId { get; set; }

        public static ConversationAvatar Create(byte[] fileData,
byte[] thumbnail,
string fileName,
decimal filesize
, string extension, Guid UserID, Guid conversationId) => new ConversationAvatar
{
    Id = Guid.NewGuid(),
    Deleted = false,
    CreateDate = DateTime.Now,
    ConversationId = conversationId,
    FileData = fileData,
    Thumbnail = thumbnail,
    FileName = fileName,
    FileSize = filesize,
    CreatedByUserId = UserID,
    Extension = extension
};

    }
}
