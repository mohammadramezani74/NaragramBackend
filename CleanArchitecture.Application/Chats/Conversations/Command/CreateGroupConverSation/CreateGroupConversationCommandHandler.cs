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

namespace CleanArchitecture.Application.Chats.Conversations.Command.CreateGroupConverSation
{
    internal sealed class CreateGroupConversationCommandHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork uow) : ICommandHandler<CreateGroupConversationCommand>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(CreateGroupConversationCommand request, CancellationToken cancellationToken)
        {
            var AllExists = request.Others.All( u =>  _userManager.ExistUserBy(u));
            var me = await _userManager.GetUserBy(_userManager.UserId!.Value);
     
            var others=await _uow.Users.Where(x=>request.Others.Contains(x.Id)).ToListAsync();
            var group = Domain.Entities.Chat.Conversation.Create(me!, others,request.Title,request.Description,request.UserName);
        
            _uow.Conversation.Add(group);
           
            await _uow.SaveChangesAsync();
            return new  OperationResult().succedded();
        }
    }
}
