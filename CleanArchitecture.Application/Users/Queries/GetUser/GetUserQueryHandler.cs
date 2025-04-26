using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUser;

public class GetUserQueryHandler(IApplicationUserManager applicationUserManager) : IQueryHandler<GetUserQuery, List<GetUserResponse>>
{
    private readonly IApplicationUserManager _usermanager=applicationUserManager;

    public async Task<OperationResult<List<GetUserResponse>>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {

        var users= await _usermanager.GetUsers(request, cancellationToken);
        return users.Where(x => x.Id != _usermanager.UserId!.Value).ToList();
    }
}
