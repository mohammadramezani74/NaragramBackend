using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.Hubs.Services;
using CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;
using CleanArchitecture.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace CleanArchitecture.Presentation.RealTime.Chat
{
    public class ChatHub(IApplicationUserManager useManager, 
        ISender sender
        , UserOnlineService userOnline) : Hub
    {
         
        private readonly IApplicationUserManager _useManager = useManager;
        private readonly ISender _sender = sender;
        private readonly UserOnlineService userOnline = userOnline;
     
       public async Task SendMessage(Guid ConversaionId, string Message)
        {
            var currentUserId = _useManager.UserId;
            if (currentUserId is not null)
            {
                //await _sender.Send(new CreateMessageCommand(ConversaionId, Message));
            }
       }
        public override async Task OnConnectedAsync()
        {
            if (Context.User?.Identity?.IsAuthenticated ?? false)
            {
                var currentUserId = _useManager.UserId;
                if (currentUserId is not null)
                {
                    userOnline.AddOnlineUsers(currentUserId.Value, Context.ConnectionId);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            userOnline.RemoveByConnectionId( Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}

