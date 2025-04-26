using CleanArchitecture.Application.Users.Commands.CreateUser;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Command.CreatMessage
{
    public sealed class CreateMessageCommandValidator : AbstractValidator<CreateMessageCommand>
    {
        public CreateMessageCommandValidator()
        {
            RuleFor(x => x.Message)
       .NotEmpty().WithMessage("Message cannot be empty.") 
       .MaximumLength(4096).WithMessage("Message cannot exceed 4096 characters.");
        }
    }
}
