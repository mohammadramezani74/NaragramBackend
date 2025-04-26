using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.LoginOrRegister
{
    internal class LoginOrRegisterCommandHandler(IApplicationUserManager userManager) : ICommandHandler<LoginOrRegisterCommand,TokenResponse>
    {
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<TokenResponse>> Handle(LoginOrRegisterCommand request, CancellationToken cancellationToken)
        {
          var result=  await _userManager.CreateOrLoginUserAsync(request.phoneNumber,request.verifyCode);
            return result;
        }
    }
}
