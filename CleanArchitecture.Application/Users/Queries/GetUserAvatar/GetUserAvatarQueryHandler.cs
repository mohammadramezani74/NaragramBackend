using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUserAvatar
{
    internal sealed class GetUserAvatarQueryHandler(IApplicationUnitOfWork Uow) : IQueryHandler<GetUserAvatarQuery, GetUserAvatarQueryResponse>
    {
        private readonly IApplicationUnitOfWork _uow = Uow;

        public async Task<OperationResult<GetUserAvatarQueryResponse>> Handle(GetUserAvatarQuery request, CancellationToken cancellationToken)
        {
            var avatar = await _uow.UserAvatars.AsNoTracking()
                 .Where(x => x.UserId == request.UserId)
                 .FirstOrDefaultAsync();
            if (avatar == null) {
                return new GetUserAvatarQueryResponse(null);
            }
            var base64 = Convert.ToBase64String(avatar.FileData!);
            return new GetUserAvatarQueryResponse(base64);
        }
    }
}
