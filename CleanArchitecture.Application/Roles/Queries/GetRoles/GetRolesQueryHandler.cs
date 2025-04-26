using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using Mapster;
using MapsterMapper;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Queries.GetRoles
{
    internal sealed class GetRolesQueryHandler(IApplicationRoleManager roleManager, IMapper mapper) : IQueryHandler<GetRolesQuery, List<GetRolesResponse2>>
    {
        private readonly IApplicationRoleManager _roleManager = roleManager;
        private readonly IMapper _mapper=mapper;

        public async Task<OperationResult<List<GetRolesResponse2>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {

            var Roles = await _roleManager.GetRoles(request, cancellationToken).ConfigureAwait(false);
         
           var result= _mapper.Map < List < GetRolesResponse2 >> (Roles);
            return new OperationResult<List<GetRolesResponse2>>(result, new OperationResult().succedded(), Roles.Count);
        }
    }
}
