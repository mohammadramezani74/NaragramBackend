using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Abstraction;

namespace CleanArchitecture.Application.Channels.Command.ChannelMessage
{
    internal sealed class SendChannelMessageCommandHandler(IApplicationUnitOfWork uow
        ,IApplicationUserManager usermanager,
        IHubContext<NaraHub, IChatHubClient> hubContext,
        INotificationService notifService) : ICommandHandler<SendChannelMessageCommand,Guid>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _usermanager = usermanager;
        private readonly INotificationService _notifService = notifService;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult<Guid>> Handle(SendChannelMessageCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var MyId = _usermanager.UserId!.Value;
            var AllMembers=await _uow.ChannelMembers.Where(x=>x.ChannelId == request.ChannelId).ToListAsync();
            foreach (var member in AllMembers)
            {
                member.IncreaseCount();
            }
    
            var Myuser = await _usermanager.GetUserBy(MyId);
            var channel = await _uow.Channels.Include(x=>x.Members).Where(x => x.Id == request.ChannelId).FirstOrDefaultAsync(cancellationToken);
            var message = Message.AddForChannelMessage(request.Message, request.ChannelId, MyId,
                type: MessageType.Text);
            var lastmessage = message.Content;
            channel.LastMessageText = message.MessageType == MessageType.Text
                     ? (message.Content.Length > 30 ? message.Content.Substring(0, 30) + "..." : message.Content)
                 : (GetMessageFormat(message.MessageType).Length > 30 ? "..." + GetMessageFormat(message.MessageType).Substring(0, 30) : GetMessageFormat(message.MessageType));
            channel.LastMessageId = message.Id;
            channel.LastUserSenderMessageId = MyId;
            channel.LastMessageSentAt = message.CreateDate;
           
            _uow.Messages.Add(message);
            await _uow.SaveChangesAsync(cancellationToken);
            var members= channel.Members.Select(x=>x.UserId).ToList();
            var messageDto = new ChatMessageDto
            {
                Id = message.Id,
                Content = request.Message,
                SendAt = message.CreateDate,
                SenderName = Myuser.LastName + " " + Myuser.FirsName,
                Type = 0,
                UserId = channel.Id,
                ParentId = message.ParentMessageId,
            };
            try
            {
                if (members.Any()) {
                var firebaseTokens = await _uow.FireBaseTokens.AsNoTracking().Where(x=> members.Contains(x.UserId)).ToListAsync();
                if (firebaseTokens.Any())
                {
                    foreach (var token in firebaseTokens)
                    {
                       await _notifService.Send(token.Token, messageDto.Content, channel.Title);
                    }
                } }
            }
            catch (Exception ex)
            {


            }

            var excludedConnections = NaraHub.GetUserConnections(MyId);

            await _hubContext.Clients
                .GroupExcept(channel.Id.ToString(), excludedConnections)
                .IncreaseMessageCount(channel.Id);

            await _hubContext.Clients
                .GroupExcept(channel.Id.ToString(), excludedConnections)
                .MessagedReceived(messageDto);
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
    }
}
