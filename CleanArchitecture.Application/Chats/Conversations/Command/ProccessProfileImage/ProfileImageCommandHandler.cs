using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Command.ProccessProfileImage
{
    internal sealed class ProfileImageCommandHandler(IApplicationUnitOfWork uow) : ICommandHandler<ProfileImageCommand, UserAvatarResponse>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<UserAvatarResponse>> Handle(ProfileImageCommand request, CancellationToken cancellationToken)
        {
            var UserAvatar=new UserAvatarResponse();
           var images=await _uow.UserAvatars.Where
                (x=>x.UserId==request.MyUserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (images != null)
            {
                UserAvatar.MyAvatar=Convert.ToBase64String(images.FileData!);
            }
        
            var otherimage = await _uow.UserAvatars.Where
                 (x => x.UserId == request.OtherUserId)
                 .FirstOrDefaultAsync(cancellationToken);
            if (otherimage != null)
            {
                UserAvatar.OtherAvatar = Convert.ToBase64String(otherimage.FileData!);
            }
            return UserAvatar;
        }
    }
}
