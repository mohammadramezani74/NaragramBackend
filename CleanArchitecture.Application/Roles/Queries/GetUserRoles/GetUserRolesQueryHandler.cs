using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Queries.GetUserRoles
{
    public class GetUserRolesQueryHandler(IApplicationRoleManager roleManager) :IQueryHandler<GetUserRolesQuery, UserRolesResponse[]>
    {
        private readonly IApplicationRoleManager _roleManager = roleManager;

     public async Task<OperationResult<UserRolesResponse[]>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            var result = await _roleManager.GetRolesByUserId(request.UserId);
            return result;
        }
    }
}
