using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
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
        public Conversation? Conversation { get; internal set; }
        public Guid? ConversationId { get; internal set; }
        public string Content { get; internal set; } = null!;
        public bool Seen { get; internal set; }
        public MessageType MessageType { get; internal set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public Guid? ChannelId { get; set; }
        public Channel? Channel { get; set; }

        public Guid? ParentMessageId { get; set; }
        public Message? ParentMessage { get; internal set; }
        public ICollection<ChatFiles> ChatFiles { get; internal set; } = new List<ChatFiles>();
        public ICollection<Message> Replies { get; internal set; } = new List<Message>();
        public ICollection<MessageReaction> Reactions { get; private set; } = new List<MessageReaction>();
        /// <summary>
        /// نام فرستنده‌ی اصلی، فقط برای نمایش برچسب «فوروارد شده».
        ///
        /// عمداً اسنپ‌شات نام است و نه شناسه‌ی پیام مبدأ: پرش به پیام اصلی جزو
        /// نیازمندی‌ها نبود، و این‌طور پیام فوروارد شده کاملاً مستقل می‌ماند و
        /// با حذف پیام یا گفتگوی مبدأ خراب نمی‌شود.
        /// </summary>
        public string? ForwardedFromName { get; internal set; }

        public bool IsPinned { get; private set; }
        public DateTime? PinnedAt { get; private set; }
        public Guid? PinnedByUserId { get; private set; }
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
        public static Message AddForChannelMessage(string message,Guid ChannelId, Guid user, Guid? ParrentId = null, List<ChatFiles>? files = null, MessageType type = MessageType.Text)
        {

            var Message =
              new Message
              {
                  Id = Guid.NewGuid(),
                  Content = message,
                  CreateDate = DateTime.Now,
                  CreatedByUserId = user,
                  ParentMessageId = ParrentId,
                  ChatFiles = files,
                  MessageType = type,
                  ChannelId = ChannelId,
              };



            return Message;



        }
        /// <summary>
        /// برچسب مبدأ را روی پیام تازه‌ی فوروارد شده می‌گذارد.
        /// اگر پیام مبدأ خودش فوروارد بوده، همان برچسب اصلی منتقل می‌شود نه نام
        /// واسطه — یعنی زنجیره‌ی فوروارد همیشه به نفر اول اشاره می‌کند.
        /// </summary>
        public void MarkAsForwarded(string? originalLabel, string senderName)
        {
            ForwardedFromName = string.IsNullOrWhiteSpace(originalLabel)
                ? senderName
                : originalLabel;
        }

        public void Pin(Guid byUserId)
        {
            IsPinned = true;
            PinnedAt = DateTime.Now;
            PinnedByUserId = byUserId;
        }

        public void Unpin()
        {
            IsPinned = false;
            PinnedAt = null;
            PinnedByUserId = null;
        }
    }
}
