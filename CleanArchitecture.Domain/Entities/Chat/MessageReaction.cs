using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public class MessageReaction: BaseEntity
    {
        public string Reaction { get;internal set; }

        public Message Message { get;private set; }
        public Guid MessageId { get;private set; }


        public static MessageReaction CreateNewReaction(Guid userId, Guid messageId, string reaction)
            => new MessageReaction
            {
                Id = Guid.NewGuid(),
                CreateDate = DateTime.Now,
                CreatedByUserId = userId,
                MessageId = messageId,
                Reaction = reaction,
                Deleted = false,
            };
        public void changeReaction(string React)
        {
Reaction = React;
        }
    }
}
