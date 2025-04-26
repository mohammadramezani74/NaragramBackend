using CleanArchitecture.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public sealed class ChatFiles:BaseEntity
    {
        public Guid MessageId { get; set; }
        public Message Message { get; set; }
        public byte[]? FileData { get; set; }
        public byte[]? Thumbnail { get; set; }
        public string? FileName { get; set; }
        public string? Extension { get; set; }
        public int? DurationInSeconds { get; set; }
        public decimal FileSize { get; set; }

        public static ChatFiles CreateImage(byte[] imageData, byte[] thumbnail,
            string fileName, string Extension, decimal fileSize,Guid UploadedBy) => new()
            {
                Id=Guid.NewGuid(),
                Deleted=false,
                DurationInSeconds=0,
                FileSize=fileSize,
                Thumbnail=thumbnail,
                FileName=fileName,
                Extension=Extension,
                CreateDate=DateTime.Now,
                FileData=imageData,
                CreatedByUserId= UploadedBy

            };

        public static ChatFiles CreateVideo(byte[] videoData, string extensions, string fileName, long size, Guid myId,int durationInSecond=0)
        => new ChatFiles
        {
            Id = Guid.NewGuid(),
            Deleted = false,
            CreateDate = DateTime.Now,
            FileData = videoData,
            CreatedByUserId= myId,
            FileSize = size,
            FileName= fileName,
            Extension= extensions,
            DurationInSeconds= durationInSecond,
            

        };
        public static ChatFiles CreateAudio(byte[] AudioData, string extensions, string fileName, long size, Guid myId, int durationInSecond = 0)
=> new ChatFiles
{
    Id = Guid.NewGuid(),
    Deleted = false,
    CreateDate = DateTime.Now,
    FileData = AudioData,
    CreatedByUserId = myId,
    FileSize = size,
    FileName = fileName,
    Extension = extensions,
    DurationInSeconds = durationInSecond,


};
        public static ChatFiles CreateDocument(byte[] DocumentData, string extensions, string fileName, long size, Guid myId, int durationInSecond = 0)
=> new ChatFiles
{
Id = Guid.NewGuid(),
Deleted = false,
CreateDate = DateTime.Now,
FileData = DocumentData,
CreatedByUserId = myId,
FileSize = size,
FileName = fileName,
Extension = extensions,
DurationInSeconds = durationInSecond,


};
    }
}
