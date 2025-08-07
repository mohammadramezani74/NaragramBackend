using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Uploader;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Common.Utilities.ImageExtension;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.UploadAvatar
{
    internal sealed class UplaodAvatarCommandHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork unitOfWork,
        IUploaderService uploaderService, IHttpContextAccessor httpContextAccessor) : ICommandHandler<UplaodAvatarCommand, string?>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _unitOfWork = unitOfWork;
        private readonly IUploaderService _uploaderService = uploaderService;
        private readonly IHttpContextAccessor _httpContextAccessor= httpContextAccessor;

        public async Task<OperationResult<string?>> Handle(UplaodAvatarCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _userManager.UserId!.Value;
            if (request.file != null) {

                var anotherFiles=await _unitOfWork.UserAvatars.Where(x=>x.UserId==currentUser).ExecuteDeleteAsync(cancellationToken);
            
                var extension=Path.GetExtension(request.file.FileName);
                var image = await ImageExtensions.ConvertFormFileToImage(request.file);
                int thumbnail_Width = 150;
                int thumbnail_Height = (int)(image.Height * (150.0 / image.Width));
                var thumbnailImage = await request.file.GetReducedImage(thumbnail_Width > 165 ? 165 : thumbnail_Width, thumbnail_Height > 165 ? 165 : thumbnail_Height);
                var ms = new MemoryStream();
                thumbnailImage.Save(ms, new BmpEncoder());
                var imageData = await ImageExtensions.ConvertFormFileToByte(request.file);
                var avatar = UserAvatar.Create(imageData, ms.ToArray(),
                  request.file.Name, request.file.Length, extension, currentUser);
                _unitOfWork.UserAvatars.Add(avatar);
                await _unitOfWork.SaveChangesAsync();
                return Convert.ToBase64String(imageData);
            }
            return null;
        }
    }
}
