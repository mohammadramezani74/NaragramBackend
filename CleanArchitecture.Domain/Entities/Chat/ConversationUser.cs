using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public sealed class ConversationUser:BaseEntity
    {
        public Conversation Conversation { get; set; }
        public Guid ConversationId { get; set; }
        public User User { get; set; }
        public Guid UserId { get; set; }
        public int UnreadCount { get; private set; }
        public bool IsPinned { get; private set; }
        public bool IsBlocked { get;private set; }

        public void UpdateLastSeenUser()
        {
                this.ModifiedDate = DateTime.Now;
        }
        public void SetPinned(bool isPinned)
        {
            this.IsPinned = isPinned;
            this.ModifiedDate = DateTime.UtcNow;
        }

        public void SetBlocked(bool isBlocked)
        {
            IsBlocked = isBlocked;
            this.ModifiedDate = DateTime.UtcNow;
        }
        public void IncreaseCount() => UnreadCount++;
        public void DecreaseCount() => UnreadCount--;
        public void EmptyCount() => UnreadCount=0;


    }
}
