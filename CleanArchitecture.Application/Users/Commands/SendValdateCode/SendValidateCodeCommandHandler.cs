using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.SendValdateCode
{
    public sealed class SendValidateCodeCommandHandler(IApplicationUserManager userManager) : ICommandHandler<SendValidateCodeCommand>
    {
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(SendValidateCodeCommand request, CancellationToken cancellationToken)
        {
           return await _userManager.SendValidateCode(request.phoneNumber);
        }
    }
}
