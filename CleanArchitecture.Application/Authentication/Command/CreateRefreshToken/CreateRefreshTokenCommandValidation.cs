using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Authentication.Command.CreateRefreshToken
{
    internal class CreateRefreshTokenCommandValidation: AbstractValidator<CreateRefreshTokenCommand>
    {
        private readonly ITokenProvider _tokenProvider;
        public CreateRefreshTokenCommandValidation(ITokenProvider tokenProvider)
        {
          
            _tokenProvider = tokenProvider;
            RuleFor(x=>x.RefreshToken).NotEmpty()
                .WithMessage("refreshToken is not set.");

      
               

        }
    }
}
