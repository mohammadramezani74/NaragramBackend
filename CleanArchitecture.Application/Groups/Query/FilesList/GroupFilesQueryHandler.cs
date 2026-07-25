using CleanArchitecture.Application.Channels.Query.FilesList;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Query.FilesList
{
    internal sealed class GroupFilesQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<GroupFilesQuery, IReadOnlyList<ChannelFileItemResponse>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<IReadOnlyList<ChannelFileItemResponse>>> Handle(GroupFilesQuery request, CancellationToken cancellationToken)
        {
           var attachedFiles= await _uow.ChatFiles.AsNoTracking().Include(x=>x.Message)
                .Where(x=>x.Message.ConversationId==request.ConversationId).Select(x => new ChannelFileItemResponse
           {
               Id = x.Id,
               FileName = x.FileName.Trim() + x.Extension
           }).ToListAsync();
            return attachedFiles;
            
        }
    }
}
