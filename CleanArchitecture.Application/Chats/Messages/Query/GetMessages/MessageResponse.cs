using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages
{
    public sealed class MessageResponse : IRegister
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SendAt { get; set; }
        public int Type { get; set; } =0 ;
        public bool IsMine  { get; set; }
        public bool IsSeen { get; set; }
        public bool isEdited { get; set; }
        public Guid? ParentId { get; set; }
        public ChatFilesDto? FileContent { get; set; }
        public string? Reaction { get; set; }

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Conversation, MessageResponse>();
                
        }
    }
}
