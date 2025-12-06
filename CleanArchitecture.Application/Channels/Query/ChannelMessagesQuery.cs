using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query
{
    public sealed record ChannelMessagesQuery(Guid ChannelId, int count = 50):IQuery<IReadOnlyList<ChannelMessageResponse>>;
    public class ChannelMessagesQueryHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager) : IQueryHandler<ChannelMessagesQuery, IReadOnlyList<ChannelMessageResponse>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<IReadOnlyList<ChannelMessageResponse>>> Handle(ChannelMessagesQuery request, CancellationToken cancellationToken)
        {
            var user = _userManager.UserId!.Value;
            var member=_uow.ChannelMembers.Where(x=>x.UserId==user&&x.ChannelId==request.ChannelId).FirstOrDefault();
            if (member != null)
            {
                if (member.UnreadCount > 0)
                {
                    member.EmptyCount();
                    await _uow.SaveChangesAsync();
                }

            }
          var messages=await  _uow.Messages.AsNoTracking()
                     .Include(x => x.ChatFiles).
                Where(x => x.ChannelId == request.ChannelId)
                .OrderByDescending(x => x.CreateDate)
                .Take(request.count)
                .Select(x => new ChannelMessageResponse
                {
                    Id = x.Id,
                    Content = x.Content,
                    SendAt = x.CreateDate,
                    SenderName = x.CreatedByUser.FirsName + " " + x.CreatedByUser.LastName,

                    isEdited = x.ModifiedDate.HasValue,
                    ParentId = x.ParentMessageId,
                    Type = (int)x.MessageType,
                    FileContent = x.ChatFiles.Select(cf => new ChatFilesDto
                    {
                        FileId = cf.Id,
                        FileName = cf.FileName,
                        FileSize = cf.FileSize.ToString()
                    }).FirstOrDefault(),
                }).ToListAsync(cancellationToken);
            return messages.OrderBy(x => x.SendAt).ToList();

        }
    }
}
