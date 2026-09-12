using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Hubs.Models;
using Microsoft.AspNetCore.Identity;
using CleanArchitecture.Application.Abstraction.Authentication;

namespace CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage
{
    internal sealed class DeleteMessageCommandHandler(IApplicationUnitOfWork uow,
        IApplicationUserManager userManager, IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<DeleteMessageCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
        {
            var myId = _userManager.UserId!.Value;
            // فایل‌های این پیام. با اشتراک فایل ممکن است همین فایل جای دیگری
            // فوروارد شده باشد، پس اول ردیف واسط پاک می‌شود و بعد فقط فایلی
            // حذف می‌شود که دیگر هیچ پیامی به آن اشاره نمی‌کند.
            //
            // شرط قبلی (AnyAsync بدون فیلتر روی پیام) در واقع می‌پرسید «آیا در
            // کل سیستم پیامی با فایل هست» که همیشه true بود؛ حالا لازم نیست.
            var fileIds = await _uow.MessageFiles
                .Where(mf => mf.MessageId == request.MessageId)
                .Select(mf => mf.ChatFileId)
                .ToListAsync(cancellationToken);

            if (fileIds.Count > 0)
            {
                await _uow.MessageFiles
                    .Where(mf => mf.MessageId == request.MessageId)
                    .ExecuteDeleteAsync(cancellationToken);

                await _uow.ChatFiles
                    .Where(f => fileIds.Contains(f.Id)
                             && !_uow.MessageFiles.Any(mf => mf.ChatFileId == f.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }
            var message = await _uow.Messages.Include(m=>m.Conversation).AsNoTracking().Where(x => x.Id == request.MessageId).FirstOrDefaultAsync();
           await _uow.Messages.Where(x=>x.Id==request.MessageId).ExecuteDeleteAsync(cancellationToken);

            // اسنپ‌شات آخرین پیام روی گفتگو/کانال با حذف پیام به‌روز نمی‌شد، پس
            // متن پیام پاک‌شده در لیست مکالمات باقی می‌ماند. حالا از روی پیام‌های
            // باقی‌مانده بازسازی می‌شود و به پیام قبلی برمی‌گردد؛ اگر پیامی نمانده
            // باشد متن خالی می‌شود و گفتگو در لیست می‌ماند.
            var snapshot = message!.ChannelId is not null
                ? await LastMessageSync.SyncChannelAsync(_uow, message.ChannelId.Value, cancellationToken)
                : await LastMessageSync.SyncConversationAsync(_uow, message.ConversationId!.Value, cancellationToken);

            await _uow.SaveChangesAsync(cancellationToken);

            var lastMessageDto = new LastMessageChangedDto
            {
                ScopeId = snapshot.ScopeId,
                MessageId = snapshot.MessageId,
                Text = snapshot.Text,
                SentAt = snapshot.SentAt
            };

            var isGroups = message.Conversation != null ? !message.Conversation.IsPrivate : false;
            if (isGroups)
            {
                var excludedConnections = NaraHub.GetUserConnections(myId);
                await _hubContext.Clients.GroupExcept(message.ConversationId.ToString(), excludedConnections)
.DeletedMessageReceived(request.MessageId);

            }
            if (message.ChannelId is null)
            {
                await _hubContext.Clients.User(request.OtherUserId.ToString()).DeletedMessageReceived(request.MessageId);
            }
            else
            {
                await _hubContext.Clients.Groups(message.ChannelId.ToString()).DeletedMessageReceived(request.MessageId);
            }

            // برخلاف DeletedMessageReceived، این یکی به فرستنده هم می‌رسد؛ لیست
            // مکالمات خودِ حذف‌کننده هم باید عوض شود.
            if (message.ChannelId is not null)
            {
                await _hubContext.Clients.Group(message.ChannelId.ToString()!)
                    .LastMessageChanged(lastMessageDto);
            }
            else if (isGroups)
            {
                await _hubContext.Clients.Group(message.ConversationId.ToString()!)
                    .LastMessageChanged(lastMessageDto);
            }
            else
            {
                var targets = new List<string> { myId.ToString(), request.OtherUserId.ToString() };

                await _hubContext.Clients.Users(targets).LastMessageChanged(lastMessageDto);
            }

            return new OperationResult().succedded();
        }
    }
}
