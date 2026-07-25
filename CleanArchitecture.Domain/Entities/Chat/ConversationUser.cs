using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
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
        public ConversationRole Role { get;  set; }

        public bool IsPinned { get; private set; }
        public bool IsBlocked { get;private set; }
        public bool IsMuted { get; private set; }
        public bool IsAdmin { get; set; }

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
        public void SetRole(ConversationRole role)
        {
            Role = role;
            ModifiedDate = DateTime.Now;
        }

        public void Mute(bool mute,Guid MutedBy)
        {
            IsMuted = mute;
            ModifiedDate = DateTime.Now;
            ModifiedById=MutedBy;
        }

        public void promoteToAdmin(Guid UserId)
        {
          IsAdmin = true;
            Role = ConversationRole.Admin;
            ModifiedDate =DateTime.Now;
            ModifiedById = UserId;
        }
        public void DemoteFromAdmin(Guid UserId)
        {
            IsAdmin = false;
            Role = ConversationRole.Member;
            ModifiedDate = DateTime.Now;
            ModifiedById = UserId;
        }
    }
}
