using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Queries.GetRoles
{
    public record GetRolesResponse(Guid Id, string Name);
    public record GetRolesResponse2(Guid Id, string Name)
    {
     
    };

}
