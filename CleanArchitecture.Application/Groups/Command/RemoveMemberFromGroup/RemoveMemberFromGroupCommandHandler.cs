using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Command.RemoveMemberFromGroup
{
    public class RemoveMemberFromGroupCommandHandler(IApplicationUnitOfWork uow,
        CleanArchitecture.Application.Abstraction.Authentication.IApplicationUserManager userManager,
        CleanArchitecture.Application.Hubs.HubRoomMembership rooms) : ICommandHandler<RemoveMemberFromGroupCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(RemoveMemberFromGroupCommand request, CancellationToken cancellationToken)
        {
            try
            {

        
            var group = await _uow.ConversationUser
                .Where(x => x.ConversationId == request.ConversationId
                &&x.UserId==request.memberId)
                .FirstOrDefaultAsync();
            if (group == null)
                return new OperationResult().Failed("گروهی یافت نشد!");

            // قبلاً هر کاربر واردشده‌ای می‌توانست هر کسی را از هر گروهی بیرون کند.
            // حالا: خودِ عضو (ترک گروه)، یا سازنده/ادمین همان گروه؛ و سازنده بیرون نمی‌شود.
            var me = userManager.UserId;
            var conversation = await _uow.Conversation.AsNoTracking()
                .Where(c => c.Id == request.ConversationId)
                .Select(c => new { c.CreatedByUserId, c.IsPrivate })
                .FirstOrDefaultAsync(cancellationToken);
            if (me is null || conversation is null || conversation.IsPrivate)
                return new OperationResult().Failed("گروهی یافت نشد!");
            if (group.UserId == conversation.CreatedByUserId || group.Role == Domain.Enums.ConversationRole.Owner)
                return new OperationResult().Failed("سازنده‌ی گروه را نمی‌شود بیرون کرد.");
            if (request.memberId != me)
            {
                var isManager = conversation.CreatedByUserId == me || await _uow.ConversationUser.AnyAsync(x =>
                    x.ConversationId == request.ConversationId && x.UserId == me
                    && (x.Role == Domain.Enums.ConversationRole.Owner || x.Role == Domain.Enums.ConversationRole.Admin), cancellationToken);
                if (!isManager)
                    return new OperationResult().Failed("فقط ادمین‌های گروه می‌توانند عضو را بیرون کنند.");
            }

                _uow.ConversationUser.Remove(group);
            var saved = await _uow.SaveChangesAsync();
            if (saved.IsFailure)
                return new OperationResult().Failed("بیرون کردن عضو انجام نشد.");
            // قبلاً عضو بیرون‌شده تا اتصال بعدی در گروه SignalR می‌ماند و پیام‌ها را زنده می‌گرفت.
            await rooms.RemovedAsync(request.ConversationId, request.memberId);
            return new OperationResult().succedded();
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
}
