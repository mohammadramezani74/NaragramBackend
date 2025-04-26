using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Queries.GetUserRoles
{
    public record UserRolesResponse(Guid RoleId ,string Name):IRequest<GetUserRolesQuery>;
  
}
