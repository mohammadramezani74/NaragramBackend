using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CleanArchitecture.Application.Chats.Messages
{
    internal sealed class MessagesQueryHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager, IHubContext<NaraHub, IChatHubClient> hubContext) : IQueryHandler<MessagesQuery, MessageResponse[]>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub,IChatHubClient> _hubContext = hubContext;

    //    public async Task<OperationResult<MessageResponse[]>> Handle(MessagesQuery request, CancellationToken cancellationToken)
    //    {
    //        try
    //        {
               
        
    //        var ConverSationMessages = await _uow.Messages.AsNoTracking()
    //              .Include(x => x.CreatedByUser)
    //              .Include(x=>x.ChatFiles)
    //              .Where(x => x.ConversationId == request.ConversationId)
    //             .OrderByDescending(x => x.CreateDate) 
    //.Take(request.count)
    //.OrderBy(x => x.CreateDate)
    //              .Select(x => new MessageResponse
    //              {
    //                  Id = x.Id,
    //                  UserId = x.CreatedByUser!.Id,
    //                  Content = x.Content,
    //                  SendAt = x.CreateDate,
    //                  SenderName = $"{x.CreatedByUser.FirsName} {x.CreatedByUser.LastName}",
    //                  IsMine = _userManager.UserId!.Value == x.CreatedByUser.Id,
    //                  IsSeen = x.Seen,
    //                  isEdited = x.ModifiedDate != null ? true : false,
    //                  ParentId=x.ParentMessageId,
    //                  Type=(int)x.MessageType,
    //                  FileContent= MapFile(x)
                      
                      
    //              }).ToListAsync() ;
    //            var otherOnreadMessages = ConverSationMessages.Where(x => x.IsMine == false
    //            && x.IsSeen == false).ToList();
    //            if(otherOnreadMessages.Count > 0) {
    //                var messagesIds = otherOnreadMessages.Select(x => x.Id).ToList();
    //                var otheruserId = otherOnreadMessages.First().UserId;
    //                var unreadMessages=await _uow.Messages.Where(x=> messagesIds.Contains(x.Id)).ToListAsync(cancellationToken);
    //            foreach (var message in unreadMessages)
    //            {
    //                message.MarkMessageAsSeen() ;
    //            }
    //                var messageSeendto = new MessageSeenDto(messagesIds, otheruserId);
    //          await  _hubContext.Clients.User(otheruserId.ToString()).SendAsync("MessageSeen", messageSeendto);
    //            await _uow.SaveChangesAsync();
    //            }
    //            return ConverSationMessages.ToArray();
    //        }
    //        catch (Exception ex)
    //        {

    //            throw;
    //        }
    //    }
        public async Task<OperationResult<MessageResponse[]>> Handle(MessagesQuery request, CancellationToken cancellationToken)
        {
            try
            {


                var myId = _userManager.UserId!.Value;
                var usersOfconversation = await _uow.Conversation
              .AsNoTracking().Include(x => x.Users)
              .Where(x => x.Id == request.ConversationId).SelectMany(x => x.Users).ToListAsync();
                var otherId = usersOfconversation.Where(x => x.UserId != myId).Select(x => x.UserId).First();
                var otherOnreadMessages = _uow.Messages.Where(x => x.Seen == false && x.CreatedByUserId == otherId&&x.ConversationId==request.ConversationId).ToList();
                if (myId == otherId) { otherOnreadMessages = new List<Message>(); }
                if (otherOnreadMessages.Count > 0)
                {
                    var messagesIds = otherOnreadMessages.Select(x => x.Id).ToList();
                    var otheruserId = otherOnreadMessages.First().CreatedByUserId;
                    var messageSeendto = new MessageSeenDto(messagesIds, otheruserId.Value);
                    try
                    {
                        await _hubContext.Clients.User(otheruserId.ToString()).MessagedSeenReceived(messagesIds);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to hub: {ex.Message}");
                    }

      

           
                    otherOnreadMessages.ForEach(x => x.MarkMessageAsSeen());
                    await _uow.SaveChangesAsync();
                }

                var ConverSationMessages = await _uow.Messages.AsNoTracking()
                    .Include(x=>x.ChatFiles)
                    .Include(x=>x.Reactions)
                    
                    .Include(x => x.CreatedByUser)
                    .Where(x => x.ConversationId == request.ConversationId)
                  .OrderByDescending(x=>x.CreateDate)
                    .Take(request.count)
                    .Select(x => new MessageResponse
                    {
                        Id = x.Id,
                        UserId = x.CreatedByUser!.Id,
                        Content = x.Content,
                        SendAt = x.CreateDate,
                        SenderName = x.CreatedByUser.FirsName + " " + x.CreatedByUser.LastName,
                        IsMine = _userManager.UserId!.Value == x.CreatedByUser.Id,
                        IsSeen = x.Seen,
                        isEdited = x.ModifiedDate.HasValue,
                        ParentId = x.ParentMessageId,
                        Type = (int)x.MessageType,
                        FileContent = x.ChatFiles.Select(cf => new ChatFilesDto
                        {
                            FileId = cf.Id,
                            FileName = cf.FileName,
                            FileSize = cf.FileSize.ToString()
                        }).FirstOrDefault(),
                        Reaction=x.Reactions.Select(x=>x.Reaction).FirstOrDefault(),
                        
                    }).ToListAsync(cancellationToken);
          


                return ConverSationMessages.OrderBy(x=>x.SendAt).ToArray();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static ChatFilesDto? MapFile(Message x)
        {
            if (x.MessageType == MessageType.Text)
                return null;
            if(x.ChatFiles.Any())
            return new ChatFilesDto { FileId=x.ChatFiles.First().Id,
            FileName=x.ChatFiles.First().FileName,
            FileSize= x.ChatFiles.First().FileSize.ToString()};
            return null;
        }
    }
}
