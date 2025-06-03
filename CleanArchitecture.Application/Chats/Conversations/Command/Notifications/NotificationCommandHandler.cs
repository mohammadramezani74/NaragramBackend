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
    public class NotificationCommandHandler(INotificationService service) : ICommandHandler<NotificationCommand>
    {
        private readonly INotificationService _service = service;

        public async Task<OperationResult> Handle(NotificationCommand request, CancellationToken cancellationToken)
        {
          await  _service.Send(request.token,request.Message,request.Name);
            return new OperationResult().succedded();
        }
    }
}
