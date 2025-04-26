using CleanArchitecture.Application.Common.Messaging;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Queries.GetUserRoles
{
    public record GetUserRolesQuery(Guid UserId) : IQuery<UserRolesResponse[]>;
  
}
