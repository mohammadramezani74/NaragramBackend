using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.GetAvatar
{
    internal class GroupAvatarQueryHandler(IApplicationUnitOfWork unitOfWork, IWebHostEnvironment env) : IQueryHandler<GroupAvatarQuery, (byte[]? Bytes, string Name)>
    {
        private readonly IApplicationUnitOfWork _unitOfWork = unitOfWork;
        private readonly IWebHostEnvironment _env = env;

        public async Task<OperationResult<(byte[]? Bytes, string Name)>> Handle(GroupAvatarQuery request, CancellationToken cancellationToken)
        {
            byte[]? thumbnail = null;
            string? name = null;
            var op = new OperationResult();

           
                var data = await _unitOfWork.ConversationAvatar.AsNoTracking()
                    .Where(x => x.ConversationId == request.FileId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (data == null)
                {
                    var defaultPath = Path.Combine(_env.ContentRootPath, "DefaultContent", "group2.svg");
                    if (!File.Exists(defaultPath))
                        return OperationResult.Failure<(byte[]?, string)>(op.Failed("AvatarNotFound"));

                    thumbnail = await File.ReadAllBytesAsync(defaultPath, cancellationToken);
                    name = "image/svg+xml";
         

                return OperationResult.Success<(byte[]?, string)>((thumbnail, name!));
                }

                thumbnail = data.FileData;

                var ext = data.Extension ?? "";
                if (!string.IsNullOrWhiteSpace(ext) && !ext.StartsWith("."))
                    ext = "." + ext;

                name = ext switch
                {
                    ".svg" => "image/svg+xml",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };
            
           

            if (thumbnail == null || thumbnail.Length == 0)
                return OperationResult.Failure<(byte[]?, string)>(op.Failed("AvatarNotFound"));


            return OperationResult.Success<(byte[]?, string)>((thumbnail, name!));
        }
    }
}
