using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Query.GetMessages
{
    public sealed class GroupMessagesQueryHandler
        : IQueryHandler<GroupMessagesQuery, MessageResponse[]>
    {
        private readonly IApplicationUnitOfWork _uow;
        private readonly IApplicationUserManager _userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext;

        public GroupMessagesQueryHandler(
            IApplicationUnitOfWork uow,
            IApplicationUserManager userManager,
            IHubContext<NaraHub, IChatHubClient> hubContext)
        {
            _uow = uow;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task<OperationResult<MessageResponse[]>> Handle(
            GroupMessagesQuery request,
            CancellationToken cancellationToken)
        {
            var myId = _userManager.UserId!.Value;

            var conversation = await _uow.Conversation
                .Include(x => x.Users)
                .FirstOrDefaultAsync(
                    x => x.Id == request.ConversationId,
                    cancellationToken);

            if (conversation is null)
                return OperationResult.Failure<MessageResponse[]>(
                    new OperationResult().NotFound("گروه یافت نشد یا حذف شده است."));
            var myMembership = conversation.Users
                .FirstOrDefault(x => x.UserId == myId);

            if (myMembership is null)
                return OperationResult.Failure<MessageResponse[]>(
                    new OperationResult().Forbiden("شما عضو این گروه نیستید."));

            if (myMembership.UnreadCount > 0)
            {
                myMembership.EmptyCount();

                await _uow.SaveChangesAsync(cancellationToken);

         
            }

            var messages = await _uow.Messages
                .AsNoTracking()
                .Where(x => x.ConversationId == request.ConversationId)
                .OrderByDescending(x => x.CreateDate)
                .Take(request.count)
                .Select(x => new MessageResponse
                {
                    Id = x.Id,
                    UserId = x.CreatedByUserId!.Value,
                    Content = x.Content,
                    SendAt = x.CreateDate,
                    SenderName =
                        x.CreatedByUser!.LastName +
                        " " +
                        x.CreatedByUser.FirsName,
                    IsMute=IsUserMute(conversation.Users, x.CreatedByUserId!.Value),
                    IsMine = x.CreatedByUserId == myId,
                    IsSeen = x.Seen,
                    isEdited = x.ModifiedDate.HasValue,
                    ParentId = x.ParentMessageId,

                    Type = (int)x.MessageType,

                    Latitude = x.Latitude,
                    Longitude = x.Longitude,

                    FileContent = x.ChatFiles
                        .Select(cf => new ChatFilesDto
                        {
                            FileId = cf.Id,
                            FileName = cf.FileName,
                            FileSize = cf.FileSize.ToString()
                        })
                        .FirstOrDefault(),
                    ConversationType=ConversationTyped.group,

                    Reaction = x.Reactions
                        .Select(r => r.Reaction)
                        .FirstOrDefault()
                        
                })
                .ToListAsync(cancellationToken);

            return messages
                .OrderBy(x => x.SendAt)
                .ToArray();
        }

        private static bool IsUserMute(ICollection<ConversationUser> users, Guid value)
        {
            var target = users.Where(x => x.UserId == value).FirstOrDefault();
            if (target != null)
                return target.IsMuted;
            return false;
        }
    }
}
