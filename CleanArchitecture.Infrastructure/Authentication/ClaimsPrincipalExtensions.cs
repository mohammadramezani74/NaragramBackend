using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CleanArchitecture.Infrastructure.Authentication
{
    internal static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal? principal)
        {
            string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(userId, out Guid parsedUserId) ?
                parsedUserId : null;
               
        }
    }
}
