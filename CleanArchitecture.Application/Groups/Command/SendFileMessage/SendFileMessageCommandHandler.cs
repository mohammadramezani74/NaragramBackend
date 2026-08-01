using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using CleanArchitecture.Application.Common.Utilities.ImageExtension;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using SixLabors.ImageSharp.Formats.Bmp;
using CleanArchitecture.Application.Common.Utilities.ConstChat;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Command.SendFileMessage
{
    public class SendFileMessageCommandHandler(IApplicationUserManager userManager,
         IApplicationUnitOfWork uow,
         IHttpContextAccessor httpContext,
         IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<SendFileMessageCommand, CreateFileMessageResponse>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IHttpContextAccessor _httpContext = httpContext;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;
        public async Task<OperationResult<CreateFileMessageResponse>> Handle(SendFileMessageCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var MyId = _userManager.UserId!.Value;
            var Myuser = await _userManager.GetUserBy(MyId);

            var conversation = await _uow.Conversation
                .Include(x => x.Users)
                                                               .SingleOrDefaultAsync
                                                               (x => x.Id == request.ConversationId, cancellationToken);
            if (conversation == null)
                return OperationResult.Failure<CreateFileMessageResponse>(op.Failed("مکالمه مورد نظر یافت نشد!"));
            if (!conversation.Users.Any(x => x.UserId == MyId))
                return OperationResult.Failure<CreateFileMessageResponse>(op.Failed("شما عضو این گفتگو نیستید!"));

            var anotherUser = conversation.Users.Where(x => x.UserId != MyId).Select(x => x.UserId).First();
            var fileExtensions = Path.GetExtension(request.file.FileName);
            var type = MessageType.Image;
            //File Agregation
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



            var message = conversation.AddMessage(request.caption ?? string.Empty, Myuser!, null, [file], type);
            var otherUser = conversation.Users.FirstOrDefault(x => x.UserId != MyId);
            if (otherUser is not null)
                otherUser.IncreaseCount();

            conversation.LastMessageText = GetMessageFormat(type);
            conversation.LastMessageId = message.Id;
            conversation.LastUserSenderMessageId = MyId;
            conversation.LastMessageSentAt = message.CreateDate;
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
                UserId = MyId,
                IsSeen = false,
                ParentId = message.ParentMessageId,
                FileContent = new ChatFilesDto
                {
                    FileId = file.Id,
                    FileName = file.FileName,
                    FileSize = file.FileSize.ToString()
                },
                ConversationType=Chats.Messages.ConversationTyped.group
                
            };

            var notification = new NotificationModelDto
                  (Myuser.LastName + " " + Myuser.FirsName,
                  SetAvatar(Myuser, hostName, scheme),
            request.caption,
            string.Empty

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
       

            return new CreateFileMessageResponse { FileId = file.Id, MessageType = type, MessageId = message.Id };
        }
        private static async Task<ChatFiles> ImageLogic(SendFileMessageCommand request, Guid MyId, string fileExtensions)
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
   $"{MyId}- {DateTime.Now.ToFileName()}",
   fileExtensions,
   request.file.Length,
   MyId
                   );
            return files;
        }
        private static async Task<ChatFiles> ProcessVideo(SendFileMessageCommand request, Guid MyId, string fileExtensions)
        {
            var videoData = await ImageExtensions.ConvertFormFileToByte(request.file);
            // var videoDuration =(int)await ImageExtensions.GetMediaDurationAsync(request.file); todo Get videoDuration
            return ChatFiles.CreateVideo(
                videoData,
                fileExtensions,
                $"{MyId}- {DateTime.Now.ToFileName()}",
                request.file.Length,
                MyId

            );
        }
        private static async Task<ChatFiles> ProcessAudio(SendFileMessageCommand request, Guid MyId, string fileExtensions)
        {
            var AudioData = await ImageExtensions.ConvertFormFileToByte(request.file);
            // var AudioDuration = (int)await ImageExtensions.GetMediaDurationAsync(request.file);
            return ChatFiles.CreateVideo(
                AudioData,
                fileExtensions,
                $"{MyId}- {DateTime.Now.ToFileName()}",
                request.file.Length,
                MyId

            );
        }
        private static async Task<ChatFiles> ProcessDocument(SendFileMessageCommand request, Guid MyId, string fileExtensions)
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
        private static string GetMessageFormat(MessageType type) => type switch
        {
            MessageType.Image => "🖼 تصویر",
            MessageType.Video => "🎬 ویدیو",
            MessageType.Audio => "🎵 صوت",
            MessageType.Document => "📎 فایل",
            _ => "پیوست"
        };
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
