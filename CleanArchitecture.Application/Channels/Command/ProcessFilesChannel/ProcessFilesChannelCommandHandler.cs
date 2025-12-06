using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Common.Utilities.ConstChat;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Application.Common.Utilities.ImageExtension;
using SixLabors.ImageSharp.Formats.Bmp;

namespace CleanArchitecture.Application.Channels.Command.ProcessFilesChannel
{
    internal sealed class ProcessFilesChannelCommandHandler(IApplicationUserManager userManager,
         IApplicationUnitOfWork uow,
         IHttpContextAccessor httpContext,
         IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<ProcessFilesChannelCommand, CreateFileMessageResponse>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IHttpContextAccessor _httpContext = httpContext;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult<CreateFileMessageResponse>> Handle(ProcessFilesChannelCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var MyId = _userManager.UserId!.Value;
            var Myuser = await _userManager.GetUserBy(MyId);

            var channel = await _uow.Channels.SingleOrDefaultAsync (x => x.Id == request.ChannelId, cancellationToken);
            if (channel == null)
                return OperationResult.Failure<CreateFileMessageResponse>(op.Failed("مکالمه مورد نظر یافت نشد!"));
            var fileExtensions = Path.GetExtension(request.file.FileName);
            var type = MessageType.Image;
            var file = new ChatFiles();
            if (SupportedChatFilesFormat.IsImage(fileExtensions))
            {
                file = await ImageLogic(request, MyId, fileExtensions);
            }
            else if (SupportedChatFilesFormat.IsVideo(fileExtensions))
            {
                type = MessageType.Video;
                file = await ProcessVideo(request, MyId, fileExtensions);
            }
            else if (SupportedChatFilesFormat.IsAudio(fileExtensions))
            {
                type = MessageType.Audio;
                file = await ProcessAudio(request, MyId, fileExtensions);
            }
            else
            {
                type = MessageType.Document;
                file = await ProcessDocument(request, MyId, fileExtensions);
            }


            var message = Message.AddForChannelMessage(request.caption ?? string.Empty,request.ChannelId, MyId, null,[file], type);
            _uow.Messages.Add(message);
            var result = await _uow.SaveChangesAsync(cancellationToken);
            var hostName = _httpContext.HttpContext.Request.Host.Value;
            var scheme = _httpContext.HttpContext.Request.Scheme;

            var messageDto = new ChatMessageDto
            {
                Id = message.Id,
                Content = request.caption,
                IsMine = false,
                SendAt = message.CreateDate,
                SenderName = Myuser.LastName + " " + Myuser.FirsName,
                Type = (int)type,
                UserId = channel.Id,
                IsSeen = false,
                ParentId = message.ParentMessageId,
                FileContent = new ChatFilesDto
                {
                    FileId = file.Id,
                    FileName = file.FileName,
                    FileSize = file.FileSize.ToString()
                }

            };
            var excludedConnections = NaraHub.GetUserConnections(MyId);

            await _hubContext.Clients.GroupExcept(channel.Id.ToString(), excludedConnections).IncreaseMessageCount(channel.Id);
            await _hubContext.Clients.GroupExcept(channel.Id.ToString(),excludedConnections).MessagedReceived(messageDto);

            return new CreateFileMessageResponse { FileId = file.Id, MessageType = type, MessageId = message.Id };
        }
        private static async Task<ChatFiles> ImageLogic(ProcessFilesChannelCommand request, Guid MyId, string fileExtensions)
        {
            var image = await ImageExtensions.ConvertFormFileToImage(request.file);
            int thumbnail_Width = (int)Math.Floor(image.Width * 0.05);
            int thumbnail_Height = (int)Math.Floor(image.Height * 0.05);
            var thumbnailImage = await request.file.GetReducedImage(thumbnail_Width > 165 ? 165 : thumbnail_Width, thumbnail_Height > 165 ? 165 : thumbnail_Height);
            var ms = new MemoryStream();
            thumbnailImage.Save(ms, new BmpEncoder());
            var imageData = await ImageExtensions.ConvertFormFileToByte(request.file);
            var files = ChatFiles.CreateImage(
                   imageData,
          ms.ToArray(),
   $"{request.file.FileName}- {DateTime.Now.ToFileName()}",
   fileExtensions,
   request.file.Length,
   MyId
                   );
            return files;
        }
        private static async Task<ChatFiles> ProcessVideo(ProcessFilesChannelCommand request, Guid MyId, string fileExtensions)
        {
            var videoData = await ImageExtensions.ConvertFormFileToByte(request.file);
            // var videoDuration =(int)await ImageExtensions.GetMediaDurationAsync(request.file); todo Get videoDuration
            return ChatFiles.CreateVideo(
                videoData,
                fileExtensions,
                $"{request.file.FileName}- {DateTime.Now.ToFileName()}",
                request.file.Length,
                MyId

            );
        }
        private static async Task<ChatFiles> ProcessAudio(ProcessFilesChannelCommand request, Guid MyId, string fileExtensions)
        {
            var AudioData = await ImageExtensions.ConvertFormFileToByte(request.file);
            // var AudioDuration = (int)await ImageExtensions.GetMediaDurationAsync(request.file);
            return ChatFiles.CreateVideo(
                AudioData,
                fileExtensions,
                $"{request.file.FileName}- {DateTime.Now.ToFileName()}",
                request.file.Length,
                MyId

            );
        }
        private static async Task<ChatFiles> ProcessDocument(ProcessFilesChannelCommand request, Guid MyId, string fileExtensions)
        {
            var DocumentData = await ImageExtensions.ConvertFormFileToByte(request.file);

            return ChatFiles.CreateDocument(
                DocumentData,
                fileExtensions,
                request.file.FileName,
                request.file.Length,
                MyId

            );
        }
    }
}
