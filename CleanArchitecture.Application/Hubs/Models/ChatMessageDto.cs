using CleanArchitecture.Application.Chats.Messages;
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

        /// <summary>
        /// شناسه‌ی گفتگویی که پیام به آن تعلق دارد: ConversationId برای چت خصوصی
        /// و گروه، ChannelId برای کانال.
        ///
        /// UserId برای این کار قابل اتکا نبود، چون معنایش بین مسیرها فرق می‌کرد:
        /// در خصوصی و گروه شناسه‌ی فرستنده است و در کانال شناسه‌ی خود کانال. به
        /// همین دلیل کلاینت نمی‌توانست بفهمد پیام مال کدام گفتگوست و پیام گروه
        /// دیگر را در گروه باز نشان می‌داد.
        /// </summary>
        public Guid ScopeId { get; set; }

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
        public ConversationTyped ConversationType { get; set; }
    }
    
}
