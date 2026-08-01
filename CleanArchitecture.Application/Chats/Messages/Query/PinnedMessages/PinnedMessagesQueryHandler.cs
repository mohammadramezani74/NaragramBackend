using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Chats.Messages.Query.PinnedMessages
{
    internal sealed class PinnedMessagesQueryHandler(
         IApplicationUnitOfWork uow,
         IApplicationUserManager userManager)
         : IQueryHandler<PinnedMessagesQuery, PinnedMessageDto[]>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<PinnedMessageDto[]>> Handle(
            PinnedMessagesQuery request, CancellationToken cancellationToken)
        {
            var myId = _userManager.UserId!.Value;

            var isMember = await _uow.ConversationUser.AsNoTracking()
                    .AnyAsync(u => u.ConversationId == request.ScopeId && u.UserId == myId, cancellationToken)
                || await _uow.ChannelMembers.AsNoTracking()
                    .AnyAsync(m => m.ChannelId == request.ScopeId && m.UserId == myId, cancellationToken);

            if (!isMember)
                return OperationResult.Failure<PinnedMessageDto[]>(
                    new OperationResult().Forbiden("دسترسی ندارید."));

            var rows = await _uow.Messages.AsNoTracking()
                .Where(m => m.IsPinned && !m.Deleted
                         && (m.ConversationId == request.ScopeId || m.ChannelId == request.ScopeId))
                .OrderByDescending(m => m.PinnedAt)
                .Select(m => new PinnedMessageDto(
                    m.Id,
                    m.Content,
                    m.CreatedByUser!.FirsName + " " + m.CreatedByUser.LastName,
                    m.CreateDate,
                    m.PinnedAt,
                    (int)m.MessageType,
                    m.ChatFiles.Select(f => f.FileName).FirstOrDefault()))
                .ToArrayAsync(cancellationToken);

            return rows;
        }
    }
}
