using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace CleanArchitecture.Infrastructure.Authentication;

internal sealed class TokenProvider(IConfiguration configuration,
    IApplicationRoleManager roleManager, UserManager<User> userManager,
    IApplicationUnitOfWork uow, IHttpContextAccessor httpContext
    
    ): ITokenProvider
{
    private readonly IApplicationRoleManager _roleManager=roleManager;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IApplicationUnitOfWork _uow = uow;
    private readonly IHttpContextAccessor _httpContext = httpContext;



    public async Task<  (string accessToken,string refreshToken)> Generate(User user)
    {
        string jwtToken = CreateJwtToken(configuration, user);
        var newRefreshToken = generateRefreshToken();
        user.AddRefreshToken(newRefreshToken);
       var result=await _userManager.UpdateAsync(user);
        return (jwtToken, newRefreshToken.HashedToken);
    }

    public async Task<(string accessToken, string refreshToken)?> RefreshToken(string token)
    {
        var user = _userManager.Users.Include(m => m.RefreshTokens).SingleOrDefault(u => u.RefreshTokens.Any(t => t.HashedToken == token));


        if (user == null) return null;

        var refreshToken = user.RefreshTokens.Single(x => x.HashedToken == token);


        if (!refreshToken.IsActive) return null;


        var newRefreshToken = generateRefreshToken();
        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = GetUseIp(); 
        refreshToken.ReplacedByToken = newRefreshToken.HashedToken;
        user.RefreshTokens.Add(newRefreshToken);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return null;
        }
 
        var jwtToken = CreateJwtToken(configuration,user);

        return (jwtToken,newRefreshToken.HashedToken);
    }
    public async Task<(bool IsRevoked, string Message)?> RevokeToken(string token)
    {
        var user = _userManager.Users.Include(m => m.RefreshTokens).SingleOrDefault(u => u.RefreshTokens.Any(t => t.HashedToken == token));

        if (user == null) return null;

        var refreshToken = user.RefreshTokens.Single(x => x.HashedToken == token);

        if (!refreshToken.IsActive) return null;


        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = GetUseIp(); 
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return null;
        }
        return (true, "توکن با موفقیت لغو شد");
    }

    private async Task< ClaimsIdentity> getClaims(User user)
    {
        var claims = new ClaimsIdentity(
         [
             new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Name, user.LastName +" "+user.FirsName!)
         ]);
        var roles = await _roleManager.GetRolesByUserId(user.Id);
        List<Guid> RoleIds=new();
        foreach (var role in roles)
        {
            claims.AddClaim(new Claim(ClaimTypes.Role, role.Name));
            RoleIds.Add(role.RoleId);
        }
        if (RoleIds.Count > 0)
        {
            foreach (var roleId in RoleIds)
            {
                var roleClaims = await _roleManager.GetClaims(roleId);
                foreach (string claim in roleClaims)
                {
                    claims.AddClaim(new Claim(ClaimTypes.Role, claim));
                }
            }

        }
 
      var  Uclaims = await _userManager.GetClaimsAsync(user);
     var   userClaims=Uclaims.Select(x=>x.Value).ToList();
        foreach (var claim in userClaims)
        {
            claims.AddClaim(new Claim(ClaimTypes.Role, claim));

        }

        return claims;
    }
    private string CreateJwtToken(IConfiguration configuration, User user)
    {
        string secretKey = configuration["Jwt:Secret"]!;
        var Securitykey = Encoding.UTF8.GetBytes(secretKey);
        var credentials = new SigningCredentials(
        new SymmetricSecurityKey(Securitykey),
        SecurityAlgorithms.HmacSha256Signature);
        var claims = getClaims(user).GetAwaiter().GetResult();
        var encryptKey = Encoding.UTF8.GetBytes(configuration["Jwt:EncryptKey"]!);
        var EncryptingCredential = new EncryptingCredentials(
            new SymmetricSecurityKey(encryptKey), SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(configuration["Jwt:ExpirationInMinutes"]!)),
            SigningCredentials = credentials,
            //EncryptingCredentials = EncryptingCredential,
            Subject = claims
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var SecyrityToken = tokenHandler.CreateToken(descriptor);
        var jwtToken = tokenHandler.WriteToken(SecyrityToken);
        return jwtToken;
    }
    private RefreshToken generateRefreshToken()
    {
        string? ipAddress = GetUseIp();
        var User_agent = _httpContext.HttpContext.Request.Headers["User-Agent"].ToString();
        var expire = DateTime.Now.AddMinutes(int.Parse(configuration["Jwt:ExpirationRefreshInMinutes"]!));
        using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
        {
            var randomBytes = new byte[64];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return new RefreshToken
            {
               
                HashedToken = Convert.ToBase64String(randomBytes),
                Expires = expire,
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                DeviceInfo = User_agent,
                
            };
        }
    }

    private string? GetUseIp()
    {
        return _httpContext.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
             _httpContext.HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
