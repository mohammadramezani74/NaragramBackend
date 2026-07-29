using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Chats.Messages
{
    internal sealed class MessagesQueryHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hubContext)
        : IQueryHandler<MessagesQuery, MessagesPage>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult<MessagesPage>> Handle(
            MessagesQuery request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var myId = _userManager.UserId!.Value;
            var take = Math.Clamp(request.Take, 10, 100);

            var conversation = await _uow.Conversation
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Id == request.ConversationId, cancellationToken);

            if (conversation is null)
                return OperationResult.Failure<MessagesPage>(op.Failed("گفتگو یافت نشد."));

            var myUser = conversation.Users.FirstOrDefault(x => x.UserId == myId);
            if (myUser is null)
                return OperationResult.Failure<MessagesPage>(op.Failed("عضو این گفتگو نیستید."));

            // علامت‌گذاری «خوانده شد» فقط در بارگذاری اول انجام می‌شود،
            // نه هر بار که کاربر به بالا اسکرول می‌کند.
            if (request.Before is null && myUser.UnreadCount > 0)
                await MarkAsSeenAsync(conversation, myId, cancellationToken);

            var query = _uow.Messages.AsNoTracking()
                .Where(m => m.ConversationId == request.ConversationId && !m.Deleted);

            if (request.Before.HasValue)
                query = query.Where(m => m.CreateDate < request.Before.Value);

            var rows = await query
                .OrderByDescending(m => m.CreateDate)
                .Take(take + 1)
                .Select(MessageProjection.ToResponse(myId))
                .ToListAsync(cancellationToken);

            var hasMore = rows.Count > take;
            if (hasMore) rows.RemoveAt(rows.Count - 1);

            // قدیمی‌ترین پیام این صفحه، cursor صفحه‌ی بعد است
            var nextCursor = rows.Count > 0 ? rows[^1].SendAt : (DateTime?)null;

            return new MessagesPage(
                [.. rows.OrderBy(m => m.SendAt)],
                nextCursor,
                hasMore);
        }

        private async Task MarkAsSeenAsync(
            Conversation conversation, Guid myId, CancellationToken ct)
        {
            var otherUser = conversation.Users.FirstOrDefault(x => x.UserId != myId);
            if (otherUser is null || otherUser.UserId == myId) return;

            var unread = await _uow.Messages
                .Where(m => m.ConversationId == conversation.Id
                         && !m.Seen
                         && m.CreatedByUserId == otherUser.UserId)
                .ToListAsync(ct);

            conversation.Users.First(x => x.UserId == myId).EmptyCount();

            if (unread.Count > 0)
            {
                var ids = unread.Select(m => m.Id).ToList();
                unread.ForEach(m => m.MarkMessageAsSeen());

                try
                {
                    await _hubContext.Clients
                        .User(otherUser.UserId.ToString())
                        .MessagedSeenReceived(ids);
                }
                catch (Exception ex)
                {
                  
                    Console.WriteLine($"Error sending to hub: {ex.Message}");
                }
            }

            await _uow.SaveChangesAsync(ct);
        }
    }
}