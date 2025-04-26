using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;

namespace CleanArchitecture.Application.Authentication.Command.CreateRefreshToken;

internal sealed class CreateRefreshTokenCommandHandler(ITokenProvider tokenProvider) : ICommandHandler<CreateRefreshTokenCommand, TokenResponse>
{
    private readonly ITokenProvider _tokenProvider=tokenProvider;

    public async Task<OperationResult<TokenResponse>> Handle(CreateRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenInfo = await _tokenProvider.RefreshToken(request.RefreshToken);
        if (!tokenInfo.HasValue)
        {
            return new TokenResponse(string.Empty, string.Empty);
        }
        return new TokenResponse(tokenInfo.Value.accessToken, tokenInfo.Value.refreshToken);
    }
}
