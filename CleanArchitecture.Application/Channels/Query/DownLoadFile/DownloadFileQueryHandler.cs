using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.DownLoadFile
{
    public class DownloadFileQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<DownloadFileQuery, string>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<string>> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
        {
            var target = await _uow.ChatFiles.Where(x => x.Id == request.FileId).FirstOrDefaultAsync(cancellationToken);
            if (target == null)
                return string.Empty;
            return Convert.ToBase64String(target.FileData!);
        }
    }
}
