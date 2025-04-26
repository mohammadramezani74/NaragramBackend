using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;

namespace CleanArchitecture.Application.Roles.Commands.AddRoleToUser;

internal class AddRoleToUserCommandHandler(IApplicationRoleManager roleManager): ICommandHandler<AddRoleToUserCommand>
{
    private readonly IApplicationRoleManager _roleManager = roleManager;

    public async Task<OperationResult> Handle(AddRoleToUserCommand request, CancellationToken cancellationToken)
    {
     var result=  await _roleManager.AddUserToRole(request.roleId,request.UserId,cancellationToken);
        return result;
    }

  
}
