using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.CreateRoleClaims
{
    internal class CreateRoleClaimsCommandHandler(IApplicationRoleManager applicationRoleManager) : ICommandHandler<CreateRoleClaimsCommand>
    {
        private readonly IApplicationRoleManager _applicationRoleManager=applicationRoleManager;
        public async Task<OperationResult> Handle(CreateRoleClaimsCommand request, CancellationToken cancellationToken)
        {
         var result= await _applicationRoleManager.CreateRoleClaimsAsync(request.RoleId,request.claims,cancellationToken);
            return result;
        }
    }
}
