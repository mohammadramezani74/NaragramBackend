using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.ChannelsAgg
{
    public class ChannelAvatar:BaseEntity
    {
        public Guid  Id { get; set; }
        public byte[]? FileData { get; private set; }
        public byte[]? Thumbnail { get; private set; }
        public string? FileName { get; private set; }
        public string? Extension { get; private set; }
        public decimal FileSize { get; private set; }
        public Channel Channel { get; set; }
        public Guid ChannelId { get; set; }
        public static ChannelAvatar Create(byte[] fileData,
    byte[] thumbnail,
    string fileName,
    decimal filesize
    , string extension, Guid UserID,Guid ChannelId) => new ChannelAvatar
    {
        Id = Guid.NewGuid(),
        Deleted = false,
        CreateDate = DateTime.Now,
        ChannelId = ChannelId,
        FileData = fileData,
        Thumbnail = thumbnail,
        FileName = fileName,
        FileSize = filesize,
        CreatedByUserId = UserID,
        Extension = extension
    };
    }
}
