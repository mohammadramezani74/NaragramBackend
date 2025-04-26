using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Command.Compelete
{
    internal class CompleteTodoCommandValidator : AbstractValidator<CompleteTodoCommand>
    {
        public CompleteTodoCommandValidator()
        {
            RuleFor(c => c.TodoItemId).NotEmpty();
        }
    }
}
