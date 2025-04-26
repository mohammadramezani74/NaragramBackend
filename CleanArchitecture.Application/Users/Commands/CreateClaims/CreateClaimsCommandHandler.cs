using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.CreateClaims
{
    internal class CreateClaimsCommandHandler(IApplicationUserManager userManager) : ICommandHandler<CreateClaimsCommand>
    {
        private readonly IApplicationUserManager _userManager=userManager;
        public async Task<OperationResult> Handle(CreateClaimsCommand request, CancellationToken cancellationToken)
        {
          var response= await _userManager.CreateUserClaimsAsync(request.UserId,request.claims,cancellationToken);
            return response;
        }
    }
}
