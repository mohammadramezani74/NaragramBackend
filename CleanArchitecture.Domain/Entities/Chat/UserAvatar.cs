using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public sealed class UserAvatar:BaseEntity
    {
        public byte[]? FileData { get; private set; }
        public byte[]? Thumbnail { get; private set; }
        public string? FileName { get; private set; }
        public string? Extension { get; private set; }
        public decimal FileSize { get; private set; }
        public User User { get; private set; }
        public Guid UserId { get; private set; }

        public static UserAvatar Create(byte[] fileData,
            byte[] thumbnail,
            string fileName,
            decimal filesize
            , string extension, Guid UserID) => new UserAvatar
            {
                Id = Guid.NewGuid(),
                Deleted = false,
                CreateDate = DateTime.Now,
                UserId = UserID,
                FileData = fileData,
                Thumbnail = thumbnail,
                FileName = fileName,
                FileSize = filesize,
                CreatedByUserId = UserID,
                Extension = extension
            };
       
    }
}
