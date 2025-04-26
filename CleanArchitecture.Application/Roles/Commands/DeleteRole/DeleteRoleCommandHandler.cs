using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.DeleteRole
{
    internal class DeleteRoleCommandHandler(IApplicationRoleManager applicationRoleManager) : ICommandHandler<DeleteRoleCommand>
    {
        private readonly IApplicationRoleManager _applicationRoleManager = applicationRoleManager;
        public async Task<OperationResult> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
           var result=await _applicationRoleManager.DeleteRole(request.Name, cancellationToken);
            return result;  
        }
    }
}
