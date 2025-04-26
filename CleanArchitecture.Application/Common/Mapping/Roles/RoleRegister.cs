using CleanArchitecture.Application.Roles.Queries.GetRoles;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Mapping.Roles
{
    public class RoleRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<GetRolesResponse, GetRolesResponse2>();
               
        }
    }
}
