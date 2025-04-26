using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Command.NewReaction
{
    internal sealed class MessageReactionCommandHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager) : ICommandHandler<MessageReactionCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(MessageReactionCommand request, CancellationToken cancellationToken)
        {
            var op=new OperationResult();
            var currentUser=_userManager.UserId!.Value;
           var message=await _uow.Messages.Include(x=>x.Reactions).FirstOrDefaultAsync(x=>x.Id==request.MessageId);
            if(message == null)
            {
                return new OperationResult().Failed("ارسال ری اکشن با خطا مواجه شد!");
            }
           var reaction= MessageReaction.CreateNewReaction(currentUser, request.MessageId, request.Reaction);
            message.ReceiveNewReactionForPrivateChats(reaction);
            await _uow.SaveChangesAsync();
            return op.succedded();
        }
    }
}
