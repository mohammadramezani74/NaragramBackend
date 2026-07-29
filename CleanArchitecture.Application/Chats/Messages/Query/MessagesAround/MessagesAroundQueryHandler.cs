using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Query.MessagesAround
{
    internal sealed class MessagesAroundQueryHandler(
          IApplicationUnitOfWork uow,
          IApplicationUserManager userManager)
          : IQueryHandler<MessagesAroundQuery, MessageResponse[]>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<MessageResponse[]>> Handle(
            MessagesAroundQuery request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var myId = _userManager.UserId!.Value;
            var take = Math.Clamp(request.Take, 5, 50);

            var anchor = await _uow.Messages.AsNoTracking()
                .Where(m => m.Id == request.MessageId)
                .Select(m => new { m.CreateDate, m.ConversationId, m.ChannelId })
                .FirstOrDefaultAsync(cancellationToken);

            if (anchor is null)
                return OperationResult.Failure<MessageResponse[]>(
                    op.Failed("پیام مورد نظر یافت نشد."));

            var isMember = anchor.ChannelId.HasValue
                ? await _uow.Channels.AnyAsync(c => c.Id == anchor.ChannelId
                        && c.Members.Any(u => u.UserId == myId), cancellationToken)
                : await _uow.Conversation.AnyAsync(c => c.Id == anchor.ConversationId
                        && c.Users.Any(u => u.UserId == myId), cancellationToken);

            if (!isMember)
                return OperationResult.Failure<MessageResponse[]>(
                    op.Failed("دسترسی به این گفتگو ندارید."));

            var scope = _uow.Messages.AsNoTracking().Where(m => !m.Deleted);
            scope = anchor.ChannelId.HasValue
                ? scope.Where(m => m.ChannelId == anchor.ChannelId)
                : scope.Where(m => m.ConversationId == anchor.ConversationId);

            // هر دو کوئری از ایندکس (ConversationId, CreateDate) استفاده می‌کنند
            var older = await scope
                .Where(m => m.CreateDate < anchor.CreateDate)
                .OrderByDescending(m => m.CreateDate)
                .Take(take)
                .Select(MessageProjection.ToResponse(myId))
                .ToListAsync(cancellationToken);

            var newerAndSelf = await scope
                .Where(m => m.CreateDate >= anchor.CreateDate)
                .OrderBy(m => m.CreateDate)
                .Take(take + 1)
                .Select(MessageProjection.ToResponse(myId))
                .ToListAsync(cancellationToken);

            return older
                .Concat(newerAndSelf)
                .OrderBy(m => m.SendAt)
                .ToArray();
        }
    }
}
