using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator:AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(u => u.FirstName)
            .NotEmpty().WithMessage("this field is Required")
            .MaximumLength(50).WithMessage("first Name must be less than 50");

        RuleFor(u => u.LastName)
        .NotEmpty().WithMessage("this field is Required")
        .MaximumLength(50).WithMessage("first Name must be less than 50");

        RuleFor(u => u.UserName)
     .NotEmpty().WithMessage("this field is Required")
     .MaximumLength(50).WithMessage("first Name must be less than 50");

        RuleFor(u => u.Password)
        .NotEmpty().WithMessage("Password is required")
        .MinimumLength(6).WithMessage("Password must be at least 6 characters long")
        .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
        ;
        RuleFor(u => u.Age)
            .NotEmpty()
    .InclusiveBetween(18, 100).WithMessage("Age must be between 18 and 100");

        RuleFor(u => u.Gender)
            .IsInEnum().WithMessage("Gender must be a valid option").NotEmpty();

        RuleFor(u => u.Address.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City name must be less than 100 characters");

    //    RuleFor(u => u.Email)
    //        .NotEmpty().WithMessage("Email is required")
    //        .EmailAddress().WithMessage("Invalid email address format");
    //        RuleFor(u => u.phoneNumber)
    //.NotEmpty().WithMessage("Phone number is required")
    //.Matches(@"^09\d{9}$").WithMessage("Phone number is not a valid Iranian mobile number");


    }
}
