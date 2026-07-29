using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages
{
    public static class MessageProjection
    {
        public static Expression<Func<Message, MessageResponse>> ToResponse(Guid myId) =>
            m => new MessageResponse
            {
                Id = m.Id,
                UserId = m.CreatedByUser!.Id,
                Content = m.Content,
                SendAt = m.CreateDate,
                SenderName = m.CreatedByUser.FirsName + " " + m.CreatedByUser.LastName,
                IsMine = m.CreatedByUser.Id == myId,
                IsSeen = m.Seen,
                isEdited = m.ModifiedDate.HasValue,
                ParentId = m.ParentMessageId,
                Type = (int)m.MessageType,
                Longitude = m.Longitude,
                Latitude = m.Latitude,
                ParentContent = m.ParentMessage != null ? m.ParentMessage.Content : null,

                ParentSenderName = m.ParentMessage != null && m.ParentMessage.CreatedByUser != null
    ? m.ParentMessage.CreatedByUser.FirsName + " " + m.ParentMessage.CreatedByUser.LastName
    : null,
                ConversationType = ConversationTyped.Private,
                FileContent = m.ChatFiles.Select(cf => new ChatFilesDto
                {
                    FileId = cf.Id,
                    FileName = cf.FileName,
                    FileSize = cf.FileSize.ToString()
                }).FirstOrDefault(),
                Reaction = m.Reactions.Select(r => r.Reaction).FirstOrDefault()
            };
    }
}

