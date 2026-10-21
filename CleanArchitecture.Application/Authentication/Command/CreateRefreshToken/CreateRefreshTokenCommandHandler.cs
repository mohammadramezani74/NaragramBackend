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
            // قبلاً «موفق» با توکن خالی برمی‌گشت؛ حالا ناموفق، تا endpoint کد 401 بدهد.
            return OperationResult.Failure<TokenResponse>(
                new OperationResult().Failed("نشست شما تمام شده است؛ دوباره وارد شوید."));
        }
        return new TokenResponse(tokenInfo.Value.accessToken, tokenInfo.Value.refreshToken);
    }
}
