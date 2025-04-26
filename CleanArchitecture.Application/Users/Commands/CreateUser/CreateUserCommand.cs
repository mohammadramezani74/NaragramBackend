using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Commands.CreateUser
{
    public record CreateUserCommand:ICommands
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? phoneNumber { get; set; }
        public int Age { get; set; }
        public Gender  Gender { get; set; }
        public Address Address { get; set; }= null!;
        public string? Email { get; set; }
    }
}
