using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public enum InboxItemKind { User = 0, Channel = 1 }
    public sealed class InboxItemDto
    {

        public InboxItemKind Kind { get; init; }

        // مشترک
        public Guid Id { get; init; }                 // برای User = UserId طرف مقابل، برای Channel = ChannelId
        public string DisplayName { get; init; } = null!;
        public string Handle { get; init; } = null!;  // @username یا @channel
        public string? Avatar { get; init; }
        public string? LastMessageText { get; init; }
        public Guid? LastMessageId { get; init; }
        public DateTime? LastMessageAt { get; init; }
        public bool IsPinned { get; init; }
        public int UnreadCount { get; init; }

        public sealed class UserBrief
        {
            public Guid UserId { get; init; }
            public DateTime? LastSeen { get; init; }
            public bool IsBlocked { get; init; }
            public bool OtherSideBlocked { get; init; }

            public sealed class ChannelBrief
            {
                public Guid ChannelId { get; init; }
                public string Title { get; init; } = null!;
                public long MemberCount { get; init; }
                public bool IsAdmin { get; init; }
                public bool CanPost { get; init; }
                public bool IsMandatoryAll { get; init; }
            }
        }
    }
}
