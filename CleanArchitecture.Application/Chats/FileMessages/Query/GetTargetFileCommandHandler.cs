using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.FileMessages.Query
{
    internal sealed class GetTargetFileCommandHandler(IApplicationUnitOfWork uow) : IQueryHandler<GetTargetFileCommand, GetFileResponse>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<GetFileResponse>> Handle(GetTargetFileCommand request, CancellationToken cancellationToken)
        {
            try
            {

         
            var targetMessage = await _uow.Messages.AsNoTracking()
                .Include(x=>x.ChatFiles)
                .Where(x => x.ChatFiles.Any(x=>x.Id == request.FileId))
                .FirstOrDefaultAsync(cancellationToken);
            if (targetMessage == null) {
                return OperationResult.Failure<GetFileResponse>(new OperationResult().BadRequest("فایلش یافت نشد!"));
            }
            var file= targetMessage.ChatFiles.First();
            return new GetFileResponse(file.FileData, file.Thumbnail, file.FileName.Trim()+ file.Extension,targetMessage.MessageType);
            }
            catch (Exception ex)
            {

                return OperationResult.Failure<GetFileResponse>(new OperationResult().BadRequest("فایل مورد نظر معتبر نمیباشد !"));

            }
        }
    }
}
