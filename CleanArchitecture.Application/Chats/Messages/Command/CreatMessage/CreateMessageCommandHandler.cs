using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Command.CreatMessage
{
    internal sealed class CreateMessageCommandHandler
        (IApplicationUserManager userManager,
         IApplicationUnitOfWork uow, 
         INotificationService notifService,
         IHttpContextAccessor httpContext,
         IHubContext<NaraHub,IChatHubClient> hubContext)
        : ICommandHandler<CreateMessageCommand,Guid>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly INotificationService _notifService = notifService;
        private readonly IHttpContextAccessor _httpContext = httpContext;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult<Guid>> Handle(CreateMessageCommand request, CancellationToken cancellationToken)
        {var op=new OperationResult();
            var MyId = _userManager.UserId!.Value;
            var Myuser=await _userManager.GetUserBy(MyId);
            var type = 0;
            if (request.Longitude != null)
            {
                type = 5;
            }
            var conversation=await _uow.Conversation
                .Include(x=>x.Users)
                                                               .SingleOrDefaultAsync
                                                               (x=>x.Id==request.ConversationId,cancellationToken);
           
            if (conversation == null)
                return OperationResult.Failure<Guid>( op.Failed("مکالمه مورد نظر یافت نشد!"));
            if (!conversation.Users.Any(x => x.UserId == MyId))
                return OperationResult.Failure<Guid>(op.Failed("شما عضو این گفتگو نیستید!"));

            var anotherUser = conversation.Users.Where(x => x.UserId != MyId).Select(x => x.UserId).First();


            var message = conversation.AddMessage(request.Message,Myuser!,request.ParentId,latitude:request.latitude,
                Longitude:request.Longitude,type:(MessageType) type);
            var lastmessage= message.Content;
            conversation.LastMessageText = message.MessageType==MessageType.Text
                     ? (message.Content.Length > 30 ? message.Content.Substring(0, 30) + "..." : message.Content)
     : (GetMessageFormat(message.MessageType).Length > 30 ? "..." + GetMessageFormat(message.MessageType).Substring(0, 30) : GetMessageFormat(message.MessageType));
            conversation.LastMessageId = message.Id;
            conversation.LastUserSenderMessageId=MyId;
            conversation.LastMessageSentAt = message.CreateDate;
            var otheruser= conversation.Users.Where(x=>x.UserId!=MyId).First();
            otheruser.IncreaseCount();
            var result=  await _uow.SaveChangesAsync(cancellationToken);
            var hostName = _httpContext.HttpContext.Request.Host.Value;
            var scheme = _httpContext.HttpContext.Request.Scheme;
          
         
            var messageDto = new ChatMessageDto {
                Id = message.Id,
                Content = request.Message,
                IsMine =false,
                SendAt = message.CreateDate,
                SenderName = Myuser.LastName + " " + Myuser.FirsName,
                Type = type,
                UserId = MyId,
                IsSeen = false,
                ParentId=message.ParentMessageId,
                Latitude = message.Latitude,
                Longitude = message.Longitude,
                ConversationType= ConversationTyped.Private
                
            };
          
    
            try
            {
                var firebaseTokens = await _uow.FireBaseTokens.AsNoTracking().Where(x => x.UserId == anotherUser).ToListAsync();
                if (firebaseTokens.Any())
                {
                    foreach (var token in firebaseTokens)
                    {
                         await _notifService.Send(token.Token, messageDto.Content, Myuser.FirsName + " " + Myuser.LastName);
                    }
                }
            }
            catch (Exception ex)
            {

              
            }
            var notification = new NotificationModelDto
            (Myuser.LastName + " " + Myuser.FirsName,
            SetAvatar(Myuser, hostName, scheme),
      request.Message,
      "https://naragram.irannara.com/chat"

          );
            await _hubContext.Clients.User(anotherUser.ToString()).ReceivedNotifications(notification);
            await _hubContext.Clients.User(anotherUser.ToString()).IncreaseMessageCount(MyId);
            await _hubContext.Clients.User(anotherUser.ToString()).MessagedReceived(messageDto);
            if (result.IsSuccess)
            {

            }
      
            return message.Id;

        }
        private static string GetMessageFormat(MessageType messageType)
        {
            string Messagetype = messageType switch
            {
                MessageType.Video => "پیام ویدیویی",
                MessageType.Audio => "پیام صوتی",
                MessageType.Image => "پیام تصویری",
                MessageType.Document => "پیام  اسنادی",
                MessageType.Location => "لوکیشن"
            };
            return Messagetype;
        }
        public static string SetAvatar(User x, string hostName, string scheme)
        {
            string RootAddress = $"{scheme}://{hostName}";
            var avatar = x.Avatar;
            if (avatar != null)
            {
                return RootAddress + avatar;
            }
            else
            {
                if (x.Gender == Domain.Enums.Gender.Male)
                {
                    return RootAddress + "/ChatFiles/Defaults/male.png";
                }
                else
                {

                    return RootAddress + "/ChatFiles/Defaults/female.png";

                }
            }
        }
    }
}
