using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Uploader;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Common.Utilities.ImageExtension;
using CleanArchitecture.Application.Users.Commands.UploadAvatar;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.UploadChannelAvatar
{
    internal sealed class UploadChannelAvatarCommandHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork unitOfWork) : ICommandHandler<UploadChannelAvatarCommand, string?>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _unitOfWork = unitOfWork;
        public async Task<OperationResult<string?>> Handle(UploadChannelAvatarCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _userManager.UserId!.Value;
            if (request.file != null)
            {

                var anotherFiles = await _unitOfWork.UserAvatars.Where(x => x.UserId == currentUser).ExecuteDeleteAsync(cancellationToken);

                var extension = Path.GetExtension(request.file.FileName);
                var image = await ImageExtensions.ConvertFormFileToImage(request.file);
                int thumbnail_Width = 150;
                int thumbnail_Height = (int)(image.Height * (150.0 / image.Width));
                var thumbnailImage = await request.file.GetReducedImage(thumbnail_Width > 165 ? 165 : thumbnail_Width, thumbnail_Height > 165 ? 165 : thumbnail_Height);
                var ms = new MemoryStream();
                thumbnailImage.Save(ms, new BmpEncoder());
                var imageData = await ImageExtensions.ConvertFormFileToByte(request.file);
                var avatar = ChannelAvatar.Create(imageData, ms.ToArray(),
                  request.file.Name, request.file.Length, extension, currentUser,request.channelId);
                _unitOfWork.ChannelAvatars.Add(avatar);
                await _unitOfWork.SaveChangesAsync();
                return Convert.ToBase64String(imageData);
            }
            return null;
        }
    }
}
