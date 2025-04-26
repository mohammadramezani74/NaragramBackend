using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public void UpdateLastSeenUser()
        {
                this.ModifiedDate = DateTime.Now;
        }
    }
}
