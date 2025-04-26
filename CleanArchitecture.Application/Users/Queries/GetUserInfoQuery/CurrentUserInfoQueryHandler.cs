using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUserInfoQuery
{
    internal sealed class CurrentUserInfoQueryHandler(IApplicationUserManager userManager, IApplicationUnitOfWork uow, IHttpContextAccessor httpContextAccessor) : IQueryHandler<CurrentUserInfoQuery, CurentUserQueryResponse>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public IApplicationUnitOfWork Uow { get; } = uow;

        public async Task<OperationResult<CurentUserQueryResponse>> Handle(CurrentUserInfoQuery request, CancellationToken cancellationToken)
        {
            if (!request.Id.HasValue)
            {
                request=new CurrentUserInfoQuery(_userManager.UserId!.Value);
            }

            var user =await _userManager.GetUserBy(request.Id!.Value);
            var avatar = await Uow.UserAvatars.Where(x => x.UserId == user.Id).FirstOrDefaultAsync(cancellationToken);
            var avatarpic = avatar?.FileData != null ?Convert.ToBase64String(avatar.FileData) : null;
            return new CurentUserQueryResponse(
                user.Id,
                user.LastName + " " + user.FirsName,
                null,
                user.bio ?? "هنوز چیزی ننوشته اید",
                user.Address?.City,
                user.PhoneNumber ?? string.Empty,
                user.Email ?? string.Empty

                );

        }
        public static string? SetAvatar(User x, string hostName, string scheme)
        {
            var thumbnail = x.UserAvatars?.FirstOrDefault()?.Thumbnail ?? null;
            if (thumbnail != null)
            {
                return Convert.ToBase64String(thumbnail);
            }
            return null;
        }
    }
}
