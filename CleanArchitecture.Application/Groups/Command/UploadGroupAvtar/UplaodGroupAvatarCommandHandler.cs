using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Common.Utilities.ImageExtension;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Bmp;

namespace CleanArchitecture.Application.Groups.Command.UploadGroupAvtar
{
    public sealed class UplaodGroupAvatarCommandHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager) : ICommandHandler<UplaodGroupAvatarCommand, string?>
    {
        private readonly IApplicationUnitOfWork _unitOfWork = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<string?>> Handle(UplaodGroupAvatarCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _userManager.UserId!.Value;
            if (request.file != null)
            {

                var anotherFiles = await _unitOfWork.ConversationAvatar.Where(x => x.ConversationId == request.GroupId).ExecuteDeleteAsync(cancellationToken);

                var extension = Path.GetExtension(request.file.FileName);
                var image = await ImageExtensions.ConvertFormFileToImage(request.file);
                int thumbnail_Width = 150;
                int thumbnail_Height = (int)(image.Height * (150.0 / image.Width));
                var thumbnailImage = await request.file.GetReducedImage(thumbnail_Width > 165 ? 165 : thumbnail_Width, thumbnail_Height > 165 ? 165 : thumbnail_Height);
                var ms = new MemoryStream();
                thumbnailImage.Save(ms, new BmpEncoder());
                var imageData = await ImageExtensions.ConvertFormFileToByte(request.file);
                var avatar = ConversationAvatar.Create(imageData, ms.ToArray(),
                  request.file.Name, request.file.Length, extension, currentUser,request.GroupId);
                _unitOfWork.ConversationAvatar.Add(avatar);
                await _unitOfWork.SaveChangesAsync();
                return Convert.ToBase64String(imageData);
            }
            return null;
        }
    }
}
