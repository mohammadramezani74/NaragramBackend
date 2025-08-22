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
    public class ChannelMessagesQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<ChannelMessagesQuery, IReadOnlyList<ChannelMessageResponse>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<IReadOnlyList<ChannelMessageResponse>>> Handle(ChannelMessagesQuery request, CancellationToken cancellationToken)
        {
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
