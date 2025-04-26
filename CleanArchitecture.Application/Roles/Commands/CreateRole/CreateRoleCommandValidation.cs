using CleanArchitecture.Application.Users.Commands.CreateUser;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.CreateRole
{
    internal class CreateRoleCommandValidation: AbstractValidator<CreateRoleCommand>
    {
        public CreateRoleCommandValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Role Name Cannt be Empty")
                .MaximumLength(50).WithMessage("Name length must be less than 50 ch")
                ;
        }
    }
}
