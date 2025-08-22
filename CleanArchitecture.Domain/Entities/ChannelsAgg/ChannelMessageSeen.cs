using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.ChannelsAgg
{
    public class ChannelMessageSeen : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }

        public Message Message { get; set; }
        public Guid MessageId { get; set; }
    }
}
