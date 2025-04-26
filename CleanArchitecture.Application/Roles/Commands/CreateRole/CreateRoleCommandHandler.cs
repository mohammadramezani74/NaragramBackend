using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;

namespace CleanArchitecture.Application.Roles.Commands.CreateRole;

internal class CreateRoleCommandHandler(IApplicationRoleManager roleManager):ICommandHandler<CreateRoleCommand>
{
    private readonly IApplicationRoleManager _roleManager=roleManager;

    public async Task<OperationResult> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var result=await _roleManager.CreateRole(request.Name,cancellationToken);
        return result;
    }
}
