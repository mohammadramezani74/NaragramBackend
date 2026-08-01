using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Purge;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Enums;

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
            var myId = _userManager.UserId!.Value;

            var channel = await _uow.Channels
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId, cancellationToken);

            if (channel is not null)
                return await DeleteChannelAsync(channel, myId, request.ChannelId, cancellationToken);

            var group = await _uow.Conversation
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId
                                       && !c.IsPrivate, cancellationToken);

            if (group is not null)
                return await DeleteGroupAsync(group, myId, request.ChannelId, cancellationToken);

            return new OperationResult().NotFound("کانال یا گروه یافت نشد.");
        }

        // ------------------------------------------------------------- کانال

        private async Task<OperationResult> DeleteChannelAsync(
            Domain.Entities.ChannelsAgg.Channel channel,
            Guid myId, Guid channelId, CancellationToken ct)
        {
            var op = new OperationResult();

            if (channel.CreatedByUserId != myId)
                return op.Forbiden("فقط سازنده می‌تواند این کانال را حذف کند.");

            var memberIds = channel.Members
                .Select(m => m.UserId)
                .Where(id => id != myId)
                .Select(id => id.ToString())
                .ToList();

            await _purger.PurgeChannelMessagesAsync(channelId, ct);

            await _uow.ChannelInvites.Where(i => i.ChannelId == channelId)
                .ExecuteDeleteAsync(ct);

            await _uow.ChannelAdmins.Where(a => a.ChannelId == channelId)
                .ExecuteDeleteAsync(ct);

            await _uow.ChannelAvatars.Where(a => a.ChannelId == channelId)
                .ExecuteDeleteAsync(ct);

            await _uow.ChannelMembers.Where(m => m.ChannelId == channelId)
                .ExecuteDeleteAsync(ct);

            // موجودیت ردیابی‌شده را جدا می‌کنیم تا با ExecuteDelete تداخل نکند
            _uow.Channels.Entry(channel).State = EntityState.Detached;

            await _uow.Channels.Where(c => c.Id == channelId)
                .ExecuteDeleteAsync(ct);

            await NotifyDeletedAsync(memberIds, channelId);

            return op.succedded("کانال با موفقیت حذف شد.");
        }

        // -------------------------------------------------------------- گروه

        private async Task<OperationResult> DeleteGroupAsync(
            Domain.Entities.Chat.Conversation group,
            Guid myId, Guid groupId, CancellationToken ct)
        {
            var op = new OperationResult();

            var me = group.Users.FirstOrDefault(u => u.UserId == myId);
            if (me is null)
                return op.Forbiden("شما عضو این گروه نیستید.");

            var isOwner = group.CreatedByUserId == myId || me.Role == ConversationRole.Owner;
            if (!isOwner)
                return op.Forbiden("فقط سازنده می‌تواند این گروه را حذف کند.");

            var memberIds = group.Users
                .Select(u => u.UserId)
                .Where(id => id != myId)
                .Select(id => id.ToString())
                .ToList();

            await _purger.PurgeConversationMessagesAsync(groupId, ct);

            await _uow.ConversationAvatar.Where(a => a.ConversationId == groupId)
                .ExecuteDeleteAsync(ct);

            await _uow.ConversationUser.Where(u => u.ConversationId == groupId)
                .ExecuteDeleteAsync(ct);

            _uow.Conversation.Entry(group).State = EntityState.Detached;

            await _uow.Conversation.Where(c => c.Id == groupId)
                .ExecuteDeleteAsync(ct);

            await NotifyDeletedAsync(memberIds, groupId);

            return op.succedded("گروه با موفقیت حذف شد.");
        }

        // ------------------------------------------------------------ کمکی

        private async Task NotifyDeletedAsync(List<string> memberIds, Guid id)
        {
            if (memberIds.Count == 0) return;

            try
            {
                await _hubContext.Clients.Users(memberIds).GetDeletedChannel(id);
            }
            catch (Exception)
            {
                // داده پاک شده؛ قطع بودن هاب نباید عملیات را ناموفق نشان دهد
            }
        }
    }
}
