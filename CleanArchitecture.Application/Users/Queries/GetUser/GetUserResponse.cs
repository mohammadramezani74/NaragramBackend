using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUser
{
    public class GetUserResponse
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public DateTime? LastSeen { get; set; }
        public int MessageUnreadedCount { get; set; }
        public string UserName { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Age { get; set; } = null!;
        public string? Avatar { get; set; } 
        public Address Address { get; set; } = null!;
        public string?  LastReceivedMessage { get; set; }
        public string? LastReceivedMessageSendDate { get; set; }
        public Guid? LastReceivedMessageId { get; set; }
        public bool IsLastReceivedMessageForMe { get; set; }
        public bool IsPin { get; set; }
        public bool IsBlocked { get; set; }
        public bool OtherUserBlocked { get; set; }
        public bool IsChannel { get; set; }
        public ChannelDto? channel { get; set; } = null;
        public DateTime? LastMessageDate { get; set; }

    }

}
