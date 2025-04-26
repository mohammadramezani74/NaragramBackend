using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
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
            //if (request is null|
            //  request!.Others.All(u => _userManager.ExistUserBy(u)))
            //{
            //    return new OperationResult().Failed("User Not Found");
            //}
            var group = Domain.Entities.Chat.Conversation.Create();
            group.AddUser(me!);
            foreach (var UserId in request.Others)
            {
                var user = await _userManager.GetUserBy(UserId);
                group.AddUser(user!);
            } 
            _uow.Conversation.Add(group);
           
            await _uow.SaveChangesAsync();
return new  OperationResult().succedded();
        }
    }
}
