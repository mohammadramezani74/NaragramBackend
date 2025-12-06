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

namespace CleanArchitecture.Application.Users.Queries.GetAvatar
{
    public class GetAvatarQueryHandler(IApplicationUnitOfWork unitOfWork, IWebHostEnvironment env) : IQueryHandler<AvatarIdQuery, (byte[]? Bytes, string Name)>
    {
        private readonly IApplicationUnitOfWork _unitOfWork = unitOfWork;
        private readonly IWebHostEnvironment _env = env;


        public async Task<OperationResult<(byte[]? Bytes, string Name)>> Handle(AvatarIdQuery request, CancellationToken cancellationToken)
        {
            byte[]? thumbnail = null;
            string? name = null;
            var op = new OperationResult();

            if (request.isfromchannel)
            {
                var data = await _unitOfWork.ChannelAvatars.AsNoTracking()
                    .Where(x => x.ChannelId == request.fileId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (data == null)
                {
                    var defaultPath = Path.Combine(_env.ContentRootPath, "DefaultContent", "chanel.png");
                    if (!File.Exists(defaultPath))
                        return OperationResult.Failure<(byte[]?, string)>(op.Failed("AvatarNotFound"));

                    thumbnail = await File.ReadAllBytesAsync(defaultPath, cancellationToken);
                    name = "image/png";
                    return OperationResult.Success<(byte[]?, string)>((thumbnail, name!));
                }

                thumbnail = data.FileData;

                var ext = data.Extension ?? "";
                if (!string.IsNullOrWhiteSpace(ext) && !ext.StartsWith("."))
                    ext = "." + ext;

                name =   ext switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                }; 
            }
            else
            {
                var data = await _unitOfWork.UserAvatars.AsNoTracking()
                    .Where(x => x.UserId == request.fileId)
                    .FirstOrDefaultAsync(cancellationToken);

              
                    if (data == null)
                    {
                        var defaultPath = Path.Combine(_env.ContentRootPath, "DefaultContent", "avatar.jpg");
                        if (!File.Exists(defaultPath))
                            return OperationResult.Failure<(byte[]?, string)>(op.Failed("AvatarNotFound"));

                        thumbnail = await File.ReadAllBytesAsync(defaultPath, cancellationToken);
                        name = "image/jpeg";
                        return OperationResult.Success<(byte[]?, string)>((thumbnail, name!));
                    }
                

                thumbnail = data.FileData;

                var ext = data.Extension ?? "";
                if (!string.IsNullOrWhiteSpace(ext) && !ext.StartsWith("."))
                    ext = "." + ext;

               name = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                }; ;
            }

            if (thumbnail == null || thumbnail.Length == 0)
                return OperationResult.Failure<(byte[]?, string)>(op.Failed("AvatarNotFound"));

        
            return OperationResult.Success<(byte[]?, string)>((thumbnail, name!));
        }

    }
}
