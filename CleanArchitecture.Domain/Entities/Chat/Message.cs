using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public sealed class Message:BaseEntity
    {
        public Conversation Conversation { get; internal set; }
        public Guid ConversationId { get; internal set; }
        public string Content { get; internal set; } = null!;
        public bool Seen { get; internal set; }
        public MessageType MessageType { get; internal set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }

        public Guid? ParentMessageId { get; set; }
        public Message? ParentMessage { get; internal set; }
        public ICollection<ChatFiles> ChatFiles { get; internal set; } = new List<ChatFiles>();
        public ICollection<Message> Replies { get; internal set; } = new List<Message>();
        public ICollection<MessageReaction> Reactions { get; private set; } = new List<MessageReaction>();
        public void MarkMessageAsSeen()
        {
      
            this.Seen = true;
        }
        public void ReceiveNewReactionForPrivateChats(MessageReaction reaction)
        {
            if(Reactions.Any(x=> x.CreatedByUserId == reaction.CreatedByUserId ))
            {

              var react=  Reactions.Where(x=>x.MessageId==reaction.MessageId).First();
                react.Reaction = reaction.Reaction;
                return;
            }
            Reactions.Add(reaction);
        }

    }
}
