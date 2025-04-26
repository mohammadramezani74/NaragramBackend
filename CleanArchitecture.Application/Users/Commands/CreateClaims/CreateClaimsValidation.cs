using CleanArchitecture.Application.Users.Commands.CreateUser;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.CreateClaims
{
    internal class CreateClaimsValidation : AbstractValidator<CreateClaimsCommand>
    {
        public CreateClaimsValidation()
        {
            RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId Is Required");

            RuleFor(x => x.claims)
           .NotEmpty().WithMessage("Claims list cannot be empty") 
           .Must(claims => claims.All(c => !string.IsNullOrWhiteSpace(c))) 
           .WithMessage("Each claim must be a non-empty string");
        }
    }
}
