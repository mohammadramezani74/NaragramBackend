using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Hubs.Models
{
    public sealed class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? SenderName { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsMine { get; set; }
        public bool IsSeen { get; set; }
        public Guid? ParentId { get; set; }
        public DateTime SendAt { get; set; }
        public int Type { get; set; } = 0;
        public ChatFilesDto? FileContent { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
    }
    
}
