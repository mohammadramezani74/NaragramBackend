using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.FilesList
{
    internal class ChannelFileQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<ChannelFilesQuery, IReadOnlyList<ChannelFileItemResponse>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<IReadOnlyList<ChannelFileItemResponse>>> Handle(ChannelFilesQuery request, CancellationToken cancellationToken)
        {
            var files = await _uow.ChatFiles.Include(x => x.Message).Where(x => x.Message.ChannelId == request.ChannelId).Select(x =>

                new ChannelFileItemResponse
                {
                    Id = x.Id,
                    FileName = x.FileName.Trim() + x.Extension
                }
            ).ToListAsync();
            return files;
        }
    }
}
