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

namespace CleanArchitecture.Application.Chats.Messages.Command.ModifiedMessage
{
    internal sealed class ModifiedMessageCommandHandler(IApplicationUnitOfWork uow
        ,IApplicationUserManager userManager,
          IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<ModifiedMessageCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(ModifiedMessageCommand request, CancellationToken cancellationToken)
        {
            var myId= _userManager.UserId!.Value;
           await _uow.Messages.Where(x=>x.Id==request.MessageId)
        .ExecuteUpdateAsync(p => p
        .SetProperty(x => x.Content, request.Message)
        .SetProperty(x => x.ModifiedDate, DateTime.Now)
        .SetProperty(x => x.ModifiedById, myId)
    );
     
      
            await _uow.SaveChangesAsync();
            var editedMessage = new EditedMessageDto(request.MessageId, request.Message);
            await _hubContext.Clients.User(request.OtherUserId.ToString()).EditedMessageReceived(editedMessage);
            return new OperationResult().succedded(); ;
        }
    }
}
