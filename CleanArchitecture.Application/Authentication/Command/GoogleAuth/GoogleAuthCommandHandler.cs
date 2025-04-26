using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Entities.Identity;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Authentication.Command.GoogleAuth
{
    internal sealed class GoogleAuthCommandHandler(UserManager<User> userManager, ITokenProvider tokenProvider) : ICommandHandler<GoogleAuthCommand, TokenResponse>
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly ITokenProvider tokenProvider = tokenProvider;

        public async Task<OperationResult<TokenResponse>> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
        {
            var payload = await VerifyGoogleToken(request);
            if (payload == null)
                return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("توکن معتبر نمیباشد!"));

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user == null)
            {
                user = User.Create(payload.Email,18, payload.Email,payload.FamilyName??"کاربر ",payload.GivenName?? payload.Email,Domain.Enums.Gender.Male,null,null);
          
                await _userManager.CreateAsync(user);
            }
          var token=await  tokenProvider.Generate(user);
            return new TokenResponse(token.accessToken, token.refreshToken);
        }
        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(GoogleAuthCommand model)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { "611292152710-4fp0hfte6n9bcf371k0c1tgs7o39qe7l.apps.googleusercontent.com" }
                };

                return await GoogleJsonWebSignature.ValidateAsync(model.IdToken, settings);
            }
            catch
            {
                return null;
            }
        }
    }
}
