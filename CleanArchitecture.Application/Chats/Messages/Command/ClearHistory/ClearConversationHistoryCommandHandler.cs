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
using CleanArchitecture.Application.Hubs.Models;

namespace CleanArchitecture.Application.Chats.Messages.Command.ClearHistory
{
    internal sealed class ClearConversationHistoryCommandHandler(
          IApplicationUnitOfWork uow,
          IApplicationUserManager userManager,
          IMessagePurger purger,
          IHubContext<NaraHub, IChatHubClient> hubContext)
          : ICommandHandler<ClearConversationHistoryCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IMessagePurger _purger = purger;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(
            ClearConversationHistoryCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var myId = _userManager.UserId!.Value;

            var conversation = await _uow.Conversation
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

            if (conversation is null)
                return op.NotFound("گفتگو یافت نشد.");

            var me = conversation.Users.FirstOrDefault(u => u.UserId == myId);
            if (me is null)
                return op.Forbiden("شما عضو این گفتگو نیستید.");

            // --- بررسی مجوز ---
            // خصوصی: هر دو طرف مالک برابرند.
            // گروه: سازنده، یا کسی که نقش Owner دارد.
            if (!conversation.IsPrivate)
            {
                var isOwner = conversation.CreatedByUserId == myId
                              || me.Role == ConversationRole.Owner;

                if (!isOwner)
                    return op.Forbiden("فقط سازنده‌ی گروه می‌تواند تاریخچه را پاک کند.");
            }

            var removed = await _purger.PurgeConversationMessagesAsync(
                request.ConversationId, cancellationToken);

            // پاک کردن فیلدهای غیرنرمال آخرین پیام، وگرنه لیست گفتگوها
            // پیامی را نشان می‌دهد که دیگر وجود ندارد.
            conversation.LastMessageText = null;
            conversation.LastMessageSentAt = null;
            conversation.LastMessageId = null;
            conversation.LastUserSenderMessageId = null;

            foreach (var member in conversation.Users)
                member.EmptyCount();

            await _uow.SaveChangesAsync(cancellationToken);

            // پاک‌کننده هم باید رویداد را بگیرد. قبلاً با Where(u => u.UserId != myId)
            // کنار گذاشته می‌شد، پس داده برای هر دو طرف پاک می‌شد ولی فقط صفحه‌ی
            // مخاطب خالی می‌شد و پاک‌کننده تا رفرش، پیام‌های لودشده‌ی قبلی را
            // می‌دید و فکر می‌کرد پاک‌سازی ناقص انجام شده.
            var targets = conversation.Users
                .Select(u => u.UserId.ToString())
                .ToList();

            if (targets.Count > 0)
            {
                try
                {
                    await _hubContext.Clients
                        .Users(targets)
                        .ChatHistoryCleared(request.ConversationId);

                    // لیست مکالمات هم باید خالی شود، وگرنه متن آخرین پیام تا
                    // رفرش باقی می‌ماند. هندلر این رویداد از قبل در کلاینت هست.
                    await _hubContext.Clients
                        .Users(targets)
                        .LastMessageChanged(new LastMessageChangedDto
                        {
                            ScopeId = request.ConversationId,
                            MessageId = null,
                            Text = string.Empty,
                            SentAt = null
                        });
                }
                catch (Exception)
                {
                    // داده پاک شده؛ قطع بودن هاب نباید عملیات را ناموفق نشان دهد
                }
            }

            return op.succedded($"{removed} پیام پاک شد.");
        }
    }
}
