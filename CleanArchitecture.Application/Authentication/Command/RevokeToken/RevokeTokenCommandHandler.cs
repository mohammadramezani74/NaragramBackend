using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Authentication.Command.RevokeToken
{
    internal sealed class RevokeTokenCommandHandler(ITokenProvider tokenProvider) : ICommandHandler<RevokeTokenCommand>
    {
        private readonly ITokenProvider _tokenProvider = tokenProvider;

        public async Task<OperationResult> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var result= await _tokenProvider.RevokeToken(request.RefreshToken);
            if(result==null) return op.Failed("توکن معتبر نمیباشد");
            return op.succedded();


        }
    }
}
