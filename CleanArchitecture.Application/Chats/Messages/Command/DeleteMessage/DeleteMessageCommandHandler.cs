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

namespace CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage
{
    internal sealed class DeleteMessageCommandHandler(IApplicationUnitOfWork uow, IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<DeleteMessageCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
        {
            var haveFile = await _uow.Messages.AnyAsync(x => x.ChatFiles.Count > 0);
            if (haveFile) {
      await _uow.ChatFiles.Where(x=>x.MessageId==request.MessageId).ExecuteDeleteAsync(cancellationToken);

            }
           await _uow.Messages.Where(x=>x.Id==request.MessageId).ExecuteDeleteAsync(cancellationToken);

            await _hubContext.Clients.User(request.OtherUserId.ToString()).DeletedMessageReceived(request.MessageId);
            await _uow.SaveChangesAsync(cancellationToken);
            return new OperationResult().succedded();
        }
    }
}
