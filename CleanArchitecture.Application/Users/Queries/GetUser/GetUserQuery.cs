using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Entities.Identity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUser
{
    //[Authorize(Roles = "Admin")]
    public record GetUserQuery(string? Search):IQuery<List<GetUserResponse>>
    {
       

    }
}
