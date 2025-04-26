using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CleanArchitecture.Application.Roles.Commands.DeleteRoleClaims
{
    public record DeleteRoleClaimsCommand(Guid RoleId,string Name):ICommands;
  
}
