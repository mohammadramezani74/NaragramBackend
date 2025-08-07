using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.Events.Chat;
using CleanArchitecture.Domain.ValueObjects.Chat;

namespace CleanArchitecture.Domain.Entities.Chat
{
    public sealed class Conversation:BaseEntity
    {
        public string Title { get; private set; } = string.Empty;
        public bool IsPrivate { get; private set; }

        public ICollection<Message> Messages { get; private set; }
        public ICollection<ConversationUser> Users { get;private set; }
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageSentAt { get; set; }
        public Guid? LastMessageId { get; set; }
        public Guid? LastUserSenderMessageId  { get; set; }

        public void AddUser(User user)
        {
            Users??=new List<ConversationUser>();
            if (IsPrivate && Users.Count>1)
            {
                throw new InvalidOperationException("Conversation Is Private!");
            }
            Users.Add(new ConversationUser
            {
                User=user,
                CreateDate=DateTime.Now,
            });
        }

        /// <summary>
        /// public chat
        /// </summary>
        /// <returns></returns>
        public static Conversation Create()
            => new Conversation
            {
                IsPrivate = false,
                Title="Say Hello",
                CreateDate = DateTime.Now,
                Deleted = false,

            };
        /// <summary>
        /// private Chat
        /// </summary>
        /// <param name="firstOne"></param>
        /// <param name="SecondsOne"></param>
        /// <returns></returns>
        public static Conversation Create(User firstOne,User SecondsOne)
     => new Conversation
     {
         IsPrivate = true,
         Title = SecondsOne.FirsName+" "+SecondsOne.LastName,
         CreateDate = DateTime.Now,
         Deleted = false,
         Users=new List<ConversationUser>
         {
             new ConversationUser
             {
                 User=firstOne,
                 CreateDate = DateTime.Now,
                 Deleted=false,
             },
                   new ConversationUser
             {
                 User=SecondsOne,
                 CreateDate = DateTime.Now,
                 Deleted=false,
             }
         }

     };
        public Message AddMessage(string message,User user,Guid? ParrentId=null,List<ChatFiles>? files=null,MessageType type=MessageType.Text
            ,float? latitude=null,float? Longitude = null)
        {
            Messages ??= new List<Message>(); 
         var Message=
           new Message
            {Id=Guid.NewGuid(),
                Content = message,
                CreateDate = DateTime.Now,
                CreatedByUser=user,
                ParentMessageId=ParrentId,
                ChatFiles=files,
                MessageType= type,
                Latitude= latitude,
                Longitude= Longitude


           };
            Messages.Add(Message);
            return Message;



        }
   


   
   

    }
}
