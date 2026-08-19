using CleanArchitecture.Application.Chats.Messages;
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

        /// <summary>
        /// همیشه Channel. لازم است چون کلاینت‌های قدیمی پیام زنده را با مقایسه‌ی
        /// همین فیلد با پیام‌های لودشده تطبیق می‌دهند؛ اگر فقط سمت هاب ست شود و
        /// اینجا نه، دو طرف نامساوی می‌شوند و پیام زنده‌ی کانال از کار می‌افتد.
        /// </summary>
        public ConversationTyped ConversationType { get; set; } = ConversationTyped.Channel;
    }
}
