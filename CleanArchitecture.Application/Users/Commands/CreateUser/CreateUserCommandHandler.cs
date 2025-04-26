using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.CreateUser
{
    internal class CreateUserCommandHandler(IApplicationUserManager userManager) : ICommandHandler<CreateUserCommand>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        public async Task<OperationResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
          var response= await _userManager.CreateUserAsync(request, cancellationToken);
            return response;
        }
    }
}
