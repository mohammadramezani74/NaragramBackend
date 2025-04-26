using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Roles.Commands.CreateRoleClaims
{
    public record CreateRoleClaimsCommand(Guid RoleId,List<string> claims):ICommands;
  
}
