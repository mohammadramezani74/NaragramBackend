using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Queries.GetClaims
{
    internal sealed class GetRoleClaimsQueryHandler(IApplicationRoleManager roleManager) : IQueryHandler<GetRoleClaimsQuery,List<string>>
    {
        readonly IApplicationRoleManager _roleManager=roleManager;

        public async Task<OperationResult<List<string>>> Handle(GetRoleClaimsQuery request, CancellationToken cancellationToken)
        {
            var result = await _roleManager.GetClaims(request.RoleId);
            return new OperationResult<List<string>>(result,new OperationResult().succedded(),result.Count);
        }
    }
}
