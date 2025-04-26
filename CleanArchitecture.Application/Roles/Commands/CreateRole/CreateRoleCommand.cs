using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.CreateRole
{
    public record CreateRoleCommand(string Name):ICommands;
  
}
