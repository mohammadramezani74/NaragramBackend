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

namespace CleanArchitecture.Application.Chats.Conversations.Command.PinConversation
{
    internal class PinConversationCommandHandler(IApplicationUnitOfWork uow,
        IApplicationUserManager userManager) : ICommandHandler<PinConversationCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(PinConversationCommand request, CancellationToken cancellationToken)
        {
            var op=new OperationResult();
            var currentUserId = _userManager.UserId!.Value;
            var targetConversation =await _uow.Conversation
                 .Include(x => x.Users)
                 .Where(x => x.Id == request.ConversationId)
                 .FirstOrDefaultAsync();
            if(targetConversation is null)
            {
                return op.Failed("مکالمه مورد نظر یافت نشد");
            }
            if (request.IsPin)
            {
                var pinnedCount = await _uow.Conversation
           .Where(c => c.Users.Any(u => u.UserId == currentUserId && u.IsPinned))
           .CountAsync(cancellationToken);
                if (pinnedCount >= 5)
                    return op.Failed("امکان پین کردن بیش از 5 شخص وجود ندارد");
            }
            var myUser = targetConversation.Users.Where(x => x.UserId == currentUserId).FirstOrDefault();
            if(myUser is null) return      op.Failed("مکالمه مورد نظر یافت نشد"); ;
            myUser.SetPinned(request.IsPin);
           await _uow.SaveChangesAsync();
            return op.succedded();
        }
    }
}
