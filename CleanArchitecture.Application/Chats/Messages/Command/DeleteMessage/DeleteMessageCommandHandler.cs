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
            var haveFile = await _uow.Messages.AnyAsync(x => x.ChatFiles.Count > 0);
            if (haveFile) {
      await _uow.ChatFiles.Where(x=>x.MessageId==request.MessageId).ExecuteDeleteAsync(cancellationToken);

            }
            var message = await _uow.Messages.Include(m=>m.Conversation).AsNoTracking().Where(x => x.Id == request.MessageId).FirstOrDefaultAsync();
           await _uow.Messages.Where(x=>x.Id==request.MessageId).ExecuteDeleteAsync(cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

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
            return new OperationResult().succedded();
        }
    }
}
