using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Users.Queries.GetUserAvatar;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.ChannelAvatar
{
    internal class GetChannelAvatarQueryHandler(IApplicationUnitOfWork Uow) : IQueryHandler<GetChannelAvatarQuery, GetUserAvatarQueryResponse>
    {
        private readonly IApplicationUnitOfWork _uow = Uow;

        public async Task<OperationResult<GetUserAvatarQueryResponse>> Handle(GetChannelAvatarQuery request, CancellationToken cancellationToken)
        {
            var avatar = await _uow.ChannelAvatars.AsNoTracking()
           .Where(x => x.ChannelId == request.ChannelId)
           .FirstOrDefaultAsync();
            if (avatar == null)
            {
                return new GetUserAvatarQueryResponse(null);
            }
            var base64 = Convert.ToBase64String(avatar.FileData!);
            return new GetUserAvatarQueryResponse(base64);
        }
    }
}
