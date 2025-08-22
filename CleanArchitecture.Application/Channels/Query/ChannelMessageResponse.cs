using CleanArchitecture.Application.Hubs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query
{
    public class ChannelMessageResponse
    {
        public Guid Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SendAt { get; set; }
        public int Type { get; set; } = 0;
        public bool isEdited { get; set; }
        public Guid? ParentId { get; set; }
        public ChatFilesDto? FileContent { get; set; }
    }
}
