using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.DeleteRoleClaims
{
    internal class DeleteRoleClaimsCommandHandler(IApplicationRoleManager roleManager) : ICommandHandler<DeleteRoleClaimsCommand>
    {
        private readonly IApplicationRoleManager _roleManager=roleManager;
        public async Task<OperationResult> Handle(DeleteRoleClaimsCommand request, CancellationToken cancellationToken)
        {
            var result =await _roleManager.DeleteRoleClaimsAsync(request.RoleId, request.Name);
            return result;

        }
    }
}
