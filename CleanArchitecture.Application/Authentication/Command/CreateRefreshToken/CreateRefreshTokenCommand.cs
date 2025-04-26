using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Messaging;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Authentication.Command.CreateRefreshToken
{
    public record CreateRefreshTokenCommand(string RefreshToken):ICommands<TokenResponse>;

}
