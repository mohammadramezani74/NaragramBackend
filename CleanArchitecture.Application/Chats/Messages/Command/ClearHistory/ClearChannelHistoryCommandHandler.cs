using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Purge;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Chats.Messages.Command.ClearHistory
{
    internal sealed class ClearChannelHistoryCommandHandler(
      IApplicationUnitOfWork uow,
      IApplicationUserManager userManager,
      IMessagePurger purger,
      IHubContext<NaraHub, IChatHubClient> hubContext)
      : ICommandHandler<ClearChannelHistoryCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IMessagePurger _purger = purger;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(
            ClearChannelHistoryCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var myId = _userManager.UserId!.Value;

            var channel = await _uow.Channels
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

            if (channel is null)
                return op.NotFound("کانال یافت نشد.");

            if (channel.CreatedByUserId != myId)
                return op.Forbiden("فقط سازنده‌ی کانال می‌تواند تاریخچه را پاک کند.");

            var removed = await _purger.PurgeChannelMessagesAsync(
                request.ChannelId, cancellationToken);

            channel.LastMessageText = null;
            channel.LastMessageSentAt = null;
            channel.LastMessageId = null;
            channel.LastUserSenderMessageId = null;

            foreach (var member in channel.Members)
                member.EmptyCount();

            await _uow.SaveChangesAsync(cancellationToken);

            var others = channel.Members
                .Where(m => m.UserId != myId)
                .Select(m => m.UserId.ToString())
                .ToList();

            if (others.Count > 0)
            {
                try
                {
                    await _hubContext.Clients
                        .Users(others)
                        .ChatHistoryCleared(request.ChannelId);
                }
                catch (Exception) { }
            }

            return op.succedded($"{removed} پیام پاک شد.");
        }
    }
}
