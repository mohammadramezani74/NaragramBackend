using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Authentication.Command.ProcessToken
{
    internal sealed class ProccessTokenCommandHandler(ITokenProvider tokenProvider
    , IApplicationUserManager userManager
    , IMediator mediator) : ICommandHandler<ProccessTokenCommand, TokenResponse>
    {
        private readonly ITokenProvider _tokenProvider = tokenProvider;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IMediator _mediator = mediator;
        public async Task<OperationResult<TokenResponse>> Handle(ProccessTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.GetUserBy(request.UserName, request.password);
            if (user == null) { return new TokenResponse(string.Empty, string.Empty); }
            var token = await _tokenProvider.Generate(user);


            return new TokenResponse(token.accessToken, token.refreshToken);
        }
    }
}
