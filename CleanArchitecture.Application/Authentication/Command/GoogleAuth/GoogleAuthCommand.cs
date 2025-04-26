using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Authentication.Command.GoogleAuth
{
    public sealed record GoogleAuthCommand(string IdToken):ICommands<TokenResponse>;
}
