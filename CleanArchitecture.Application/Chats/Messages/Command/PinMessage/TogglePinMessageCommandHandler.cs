using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Chats.Messages.Command.PinMessage
{
    internal sealed class TogglePinMessageCommandHandler(
           IApplicationUnitOfWork uow,
           IApplicationUserManager userManager,
           IHubContext<NaraHub, IChatHubClient> hubContext)
           : ICommandHandler<TogglePinMessageCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        // سقف پین همزمان. بدون آن نوار بالای چت بی‌نهایت رشد می‌کند.
        private const int MaxPinned = 5;

        public async Task<OperationResult> Handle(
            TogglePinMessageCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var myId = _userManager.UserId!.Value;

            var message = await _uow.Messages
                .FirstOrDefaultAsync(m => m.Id == request.MessageId && !m.Deleted, cancellationToken);

            if (message is null)
                return op.NotFound("پیام یافت نشد.");

            var (allowed, reason) = await CanPinAsync(message, myId, cancellationToken);
            if (!allowed)
                return op.Forbiden(reason);

            if (request.Pin)
            {
                if (message.IsPinned)
                    return op.succedded("این پیام از قبل پین شده است.");

                var pinnedCount = await _uow.Messages.CountAsync(m =>
                    m.IsPinned && !m.Deleted &&
                    (message.ChannelId.HasValue
                        ? m.ChannelId == message.ChannelId
                        : m.ConversationId == message.ConversationId),
                    cancellationToken);

                if (pinnedCount >= MaxPinned)
                    return op.Failed($"حداکثر {MaxPinned} پیام می‌توانید پین کنید. ابتدا یکی را بردارید.");

                message.Pin(myId);
            }
            else
            {
                message.Unpin();
            }

            await _uow.SaveChangesAsync(cancellationToken);

            await NotifyAsync(message, request.Pin, myId, cancellationToken);

            return op.succedded(request.Pin ? "پیام پین شد." : "پین برداشته شد.");
        }

        /// <summary>
        /// خصوصی: هر دو طرف. گروه: سازنده یا ادمین. کانال: سازنده یا ادمین.
        /// </summary>
        private async Task<(bool, string)> CanPinAsync(
            Domain.Entities.Chat.Message message, Guid myId, CancellationToken ct)
        {
            if (message.ChannelId.HasValue)
            {
                var channel = await _uow.Channels.AsNoTracking()
                    .Where(c => c.Id == message.ChannelId)
                    .Select(c => new
                    {
                        c.CreatedByUserId,
                        IsMember = c.Members.Any(m => m.UserId == myId),
                        IsAdmin = c.Admins.Any(a => a.UserId == myId)
                    })
                    .FirstOrDefaultAsync(ct);

                if (channel is null || !channel.IsMember)
                    return (false, "شما عضو این کانال نیستید.");

                return channel.CreatedByUserId == myId || channel.IsAdmin
                    ? (true, string.Empty)
                    : (false, "فقط مدیران کانال می‌توانند پیام پین کنند.");
            }

            var conversation = await _uow.Conversation.AsNoTracking()
                .Where(c => c.Id == message.ConversationId)
                .Select(c => new
                {
                    c.IsPrivate,
                    c.CreatedByUserId,
                    Me = c.Users.FirstOrDefault(u => u.UserId == myId)
                })
                .FirstOrDefaultAsync(ct);

            if (conversation?.Me is null)
                return (false, "شما عضو این گفتگو نیستید.");

            if (conversation.IsPrivate)
                return (true, string.Empty);

            var isManager = conversation.CreatedByUserId == myId
                         || conversation.Me.Role == ConversationRole.Owner
                         || conversation.Me.Role == ConversationRole.Admin;

            return isManager
                ? (true, string.Empty)
                : (false, "فقط مدیران گروه می‌توانند پیام پین کنند.");
        }

        private async Task NotifyAsync(
            Domain.Entities.Chat.Message message, bool pinned, Guid myId, CancellationToken ct)
        {
            List<string> targets;

            if (message.ChannelId.HasValue)
            {
                targets = await _uow.ChannelMembers.AsNoTracking()
                    .Where(m => m.ChannelId == message.ChannelId && m.UserId != myId)
                    .Select(m => m.UserId.ToString())
                    .ToListAsync(ct);
            }
            else
            {
                targets = await _uow.ConversationUser.AsNoTracking()
                    .Where(u => u.ConversationId == message.ConversationId && u.UserId != myId)
                    .Select(u => u.UserId.ToString())
                    .ToListAsync(ct);
            }

            if (targets.Count == 0) return;

            var scopeId = message.ChannelId ?? message.ConversationId!.Value;

            try
            {
                await _hubContext.Clients.Users(targets)
                    .MessagePinChanged(scopeId, message.Id, pinned);
            }
            catch (Exception)
            {
                // داده ذخیره شده؛ قطع بودن هاب نباید عملیات را ناموفق نشان دهد
            }
        }
    }
}
