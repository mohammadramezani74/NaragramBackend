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

namespace CleanArchitecture.Application.Users.Queries.UserList
{
    public sealed record GetUsersListQuery(string?Search):IQuery<IReadOnlyList<GetUsersListResponse>>;

    public class GetUsersListQueryHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager) : IQueryHandler<GetUsersListQuery, IReadOnlyList<GetUsersListResponse>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<IReadOnlyList<GetUsersListResponse>>> Handle(GetUsersListQuery request, CancellationToken cancellationToken)
        {
            var myId = _userManager.UserId!.Value;
            var users=  _uow.Users.AsNoTracking().Where(x=>x.Id != myId);
            if(!string.IsNullOrEmpty(request.Search))
            {
                users=users.Where(x=>x.FirsName.Contains(request.Search)||
                x.LastName.Contains(request.Search));

            }
            var userList= await users.Select(x => new GetUsersListResponse
            {
                Id=x.Id,
                Name=x.FirsName+" "+x.LastName
            }).ToListAsync(cancellationToken);
            return userList;
        }
    }

}
