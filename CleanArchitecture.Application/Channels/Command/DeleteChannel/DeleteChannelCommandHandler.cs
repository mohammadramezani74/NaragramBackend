using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Purge;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Channels.Command.DeleteChannel
{
    internal sealed class DeleteChannelCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IMessagePurger purger,
        IHubContext<NaraHub, IChatHubClient> hubContext)
        : ICommandHandler<DeleteChannelCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IMessagePurger _purger = purger;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(
            DeleteChannelCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var myId = _userManager.UserId!.Value;

            var channel = await _uow.Channels
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

            if (channel is null)
                return op.NotFound("کانال یا گروه یافت نشد.");

            if (channel.CreatedByUserId != myId)
                return op.Forbiden("فقط سازنده می‌تواند این کانال یا گروه را حذف کند.");

            // اعضا را قبل از حذف نگه می‌داریم تا بعداً بتوانیم خبرشان کنیم
            var memberIds = channel.Members
                .Select(m => m.UserId.ToString())
                .Where(id => id != myId.ToString())
                .ToList();

            // ---- ۱. پیام‌ها و وابسته‌هایشان ----
            // گروه‌ها پیام‌هایشان روی ConversationId است، کانال‌ها روی ChannelId.
            // هر دو را پاک می‌کنیم چون از روی نوع مطمئن نیستیم.
            await _purger.PurgeChannelMessagesAsync(request.ChannelId, cancellationToken);
            await _purger.PurgeConversationMessagesAsync(request.ChannelId, cancellationToken);

            // ---- ۲. موجودیت‌های وابسته به کانال ----
            await _uow.ChannelMessageSeens
                .Where(s => s.Message.ChannelId == request.ChannelId)
                .ExecuteDeleteAsync(cancellationToken);

            await _uow.ChannelInvites
                .Where(i => i.ChannelId == request.ChannelId)
                .ExecuteDeleteAsync(cancellationToken);

            await _uow.ChannelAdmins
                .Where(a => a.ChannelId == request.ChannelId)
                .ExecuteDeleteAsync(cancellationToken);

            await _uow.ChannelAvatars
                .Where(a => a.ChannelId == request.ChannelId)
                .ExecuteDeleteAsync(cancellationToken);

            await _uow.ChannelMembers
                .Where(m => m.ChannelId == request.ChannelId)
                .ExecuteDeleteAsync(cancellationToken);

            // ---- ۳. اگر گروه است، Conversation متناظر ----
            var conversation = await _uow.Conversation
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

            if (conversation is not null)
            {
                await _uow.ConversationAvatar
                    .Where(a => a.ConversationId == request.ChannelId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _uow.ConversationUser
                    .Where(u => u.ConversationId == request.ChannelId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _uow.Conversation
                    .Where(c => c.Id == request.ChannelId)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // ---- ۴. خود کانال ----
            // چون Include شده و در change tracker است، اول جدایش می‌کنیم تا
            // ExecuteDelete با وضعیت ردیابی‌شده تداخل نکند.
            _uow.Channels.Entry(channel).State = EntityState.Detached;

            await _uow.Channels
                .Where(c => c.Id == request.ChannelId)
                .ExecuteDeleteAsync(cancellationToken);

            // ---- ۵. اطلاع به اعضا ----
            if (memberIds.Count > 0)
            {
                try
                {
                    await _hubContext.Clients
                        .Users(memberIds)
                        .GetDeletedChannel(request.ChannelId);
                }
                catch (Exception)
                {
                    // داده پاک شده؛ قطع بودن هاب نباید عملیات را ناموفق نشان دهد
                }
            }

            return op.succedded("با موفقیت حذف شد.");
        }
    }
}
