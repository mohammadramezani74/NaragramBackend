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

            if (!conversation.Users.Any(u => u.UserId == myId))
                return op.Forbiden("شما عضو این گفتگو نیستید.");

            // --- بررسی مجوز ---
            // گفتگوی خصوصی: هر دو طرف مالک برابرند، هر کدام می‌تواند پاک کند.
            // گروه: فقط سازنده. گروه‌ها یک Channel متناظر دارند که مالکیت
            // و ادمین‌ها آنجا نگهداری می‌شود.
            if (!conversation.IsPrivate)
            {
                var isCreator = await _uow.Channels
                    .AnyAsync(c => c.Id == request.ConversationId
                                && c.CreatedByUserId == myId, cancellationToken);

                if (!isCreator)
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

            // اطلاع به سایر اعضا تا صفحه‌شان خالی شود
            var others = conversation.Users
                .Where(u => u.UserId != myId)
                .Select(u => u.UserId.ToString())
                .ToList();

            if (others.Count > 0)
            {
                try
                {
                    await _hubContext.Clients
                        .Users(others)
                        .ChatHistoryCleared(request.ConversationId);
                }
                catch (Exception)
                {
                    // قطع بودن هاب نباید عملیات را ناموفق نشان دهد؛ داده پاک شده است
                }
            }

            return op.succedded($"{removed} پیام پاک شد.");
        }
    }
}
