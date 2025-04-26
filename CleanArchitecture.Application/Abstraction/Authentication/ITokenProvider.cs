using CleanArchitecture.Domain.Entities.Identity;

namespace CleanArchitecture.Application.Abstraction.Authentication
{
    public interface ITokenProvider
    {
       Task< (string accessToken, string refreshToken)> Generate(User user);
        Task<(string accessToken, string refreshToken)?> RefreshToken(string token);
        Task<(bool IsRevoked, string Message)?> RevokeToken(string token);
    }
}
