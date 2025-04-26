using CleanArchitecture.Application.Users.Commands.CreateUser;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.LoginOrRegister
{
    public class LoginOrRegisterCommandValidator : AbstractValidator<LoginOrRegisterCommand>

    {
        public LoginOrRegisterCommandValidator()
        {
            RuleFor(x=>x.phoneNumber)
                  .NotEmpty()
       .NotNull().WithMessage("وارد کردن شماره همراه اجباری میباشد")
       .MinimumLength(11).WithMessage("شماره همراه وارد شده معتبر نمیباشد")
       .MaximumLength(11).WithMessage("شماره همراه وارد شده معتبر نمیباشد")
       .Matches(new Regex(@"^(?:(?:(?:\\+?|00)(98))|(0))?((?:90|91|92|93|99)[0-9]{8})$")).WithMessage("PhoneNumber not valid");

        }
    }
}
