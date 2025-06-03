using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Command.Notifications
{
    public sealed class StoreNotificationCommandHandler(INotificationService service) : ICommandHandler<StoreNotificationCommand>
    {
        public async Task<OperationResult> Handle(StoreNotificationCommand request, CancellationToken cancellationToken)
        {
           await  service.StoreFCMtoken(request.Token);
            return new OperationResult().succedded();
        }
    }
}
