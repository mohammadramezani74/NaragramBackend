using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Application.Chats.Messages;

namespace CleanArchitecture.Application.Groups.Command.SendMessage
{
    internal class SendMeesageToGroupCommandHandler(IApplicationUserManager userManager,
         IApplicationUnitOfWork uow,
         INotificationService notifService,
         IHttpContextAccessor httpContext,
         IHubContext<NaraHub, IChatHubClient> hubContext)
        : ICommandHandler<SendMeesageToGroupCommand, Guid>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly INotificationService _notifService = notifService;
        private readonly IHttpContextAccessor _httpContext = httpContext;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;
        public async Task<OperationResult<Guid>> Handle(SendMeesageToGroupCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var MyId = _userManager.UserId!.Value;
            var Myuser = await _userManager.GetUserBy(MyId);
            var type = 0;
            if (request.Longitude != null)
            {
                type = 5;
            }
            var conversation = await _uow.Conversation
                .Include(x => x.Users)
                                                               .SingleOrDefaultAsync
                                                               (x => x.Id == request.ConversationId, cancellationToken);
            var myUser=conversation.Users.Where(x=>x.UserId == MyId).FirstOrDefault();
            if (myUser.IsMuted) {
                return OperationResult.Failure<Guid>(op.Failed("یوزر شما توسط ادمین سکوت خورده و امکان ارسال پیام وجود ندارد!"));
            }
            if (conversation == null)
                return OperationResult.Failure<Guid>(op.Failed("'گروه' مورد نظر یافت نشد!"));
            if (!conversation.Users.Any(x => x.UserId == MyId))
                return OperationResult.Failure<Guid>(op.Failed("شما عضو این گفتگو نیستید!"));

            var anotherUsers = conversation.Users.Where(x => x.UserId != MyId).Select(x => x.UserId).ToList();


            var message = conversation.AddMessage(request.Message, Myuser!, request.ParentId, latitude: request.latitude,
                Longitude: request.Longitude, type: (MessageType)type);
            var lastmessage = message.Content;
            conversation.LastMessageText = message.MessageType == MessageType.Text
                     ? (message.Content.Length > 15 ? message.Content.Substring(0, 15) + "..." : message.Content)
     : (GetMessageFormat(message.MessageType).Length > 15 ? "..." + GetMessageFormat(message.MessageType).Substring(0, 15) : GetMessageFormat(message.MessageType));
            conversation.LastMessageId = message.Id;
            conversation.LastUserSenderMessageId = MyId;
            conversation.LastMessageSentAt = message.CreateDate;
            var otheruser = conversation.Users.Where(x => x.UserId != MyId).ToList();
            foreach (var member in conversation.Users)
            {
                if (member.UserId != MyId)
                    member.IncreaseCount();
            }
            var result = await _uow.SaveChangesAsync(cancellationToken);
            var hostName = _httpContext.HttpContext.Request.Host.Value;
            var scheme = _httpContext.HttpContext.Request.Scheme;


            var messageDto = new ChatMessageDto
            {
                Id = message.Id,
                Content = request.Message,
                IsMine = false,
                SendAt = message.CreateDate,
                SenderName = Myuser.LastName + " " + Myuser.FirsName,
                Type = type,
                UserId = MyId,
                IsSeen = false,
                ParentId = message.ParentMessageId,
                Latitude = message.Latitude,
                Longitude = message.Longitude,
                ConversationType = ConversationTyped.group
            };


            try
            {
                var firebaseTokens = await _uow.FireBaseTokens.AsNoTracking().Where(x =>anotherUsers.Contains( x.UserId)).ToListAsync();
                if (firebaseTokens.Any())
                {
                    await Task.WhenAll(
      firebaseTokens.Select(x =>
          _notifService.Send(
              x.Token,
              messageDto.Content,
              $"{Myuser.FirsName} {Myuser.LastName}"
          ))
  );
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
            var excludedConnections = NaraHub.GetUserConnections(MyId);
            await _hubContext.Clients
                .GroupExcept(conversation.Id.ToString(), excludedConnections)
                .IncreaseMessageCount(conversation.Id);

            await _hubContext.Clients
                .GroupExcept(conversation.Id.ToString(), excludedConnections)
                .MessagedReceived(messageDto);
            await _hubContext.Clients
           .GroupExcept(conversation.Id.ToString(), excludedConnections)
           .ReceivedNotifications(notification);

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
