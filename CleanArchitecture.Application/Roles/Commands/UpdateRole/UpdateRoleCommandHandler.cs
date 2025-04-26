using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.UpdateRole
{
    internal class UpdateRoleCommandHandler(IApplicationRoleManager roleManager) : ICommandHandler<UpdateRoleCommand>
    {
        private readonly IApplicationRoleManager _roleManager=roleManager;
        public async Task<OperationResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
        var result=await   _roleManager.UpdateRole(request.oldName, request.newName);
            return result;
        }
    }
}
