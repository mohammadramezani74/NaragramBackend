using CleanArchitecture.Application.Abstraction.Authentication;
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

namespace CleanArchitecture.Application.Chats.Conversations.Command.BlockConversation
{
    internal sealed class BlockConversationCommandHandler(IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<BlockConversationCommand, bool>

    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;
        public async Task<OperationResult<bool>> Handle(BlockConversationCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _userManager.UserId!.Value;
            var targetConversation = await _uow.Conversation
                 .Include(x => x.Users)
                 .Where(x => x.Id == request.ConversationId)
                 .FirstOrDefaultAsync();
            if (targetConversation is null)
            {
                return false;
            }
            var myUser = targetConversation.Users.Where(x => x.UserId == currentUserId).FirstOrDefault();
            var otherUser = targetConversation.Users.Where(x => x.UserId != currentUserId).FirstOrDefault();
            if (myUser is null) return false;
            myUser.SetBlocked(request.IsBlock);
            await _hubContext. Clients.User(otherUser.UserId.ToString()).BlockUser(new BlockDto(myUser.UserId, request.IsBlock));

            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
