using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Query.DownloadFile
{
    public class DownloadGroupFileQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<DownloadGroupFileQuery, string>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<string>> Handle(DownloadGroupFileQuery request, CancellationToken cancellationToken)
        {
            var target = await _uow.ChatFiles.Where(x => x.Id == request.FileId).FirstOrDefaultAsync(cancellationToken);
            if (target == null)
                return string.Empty;
            return Convert.ToBase64String(target.FileData!);
        }
    }
}
