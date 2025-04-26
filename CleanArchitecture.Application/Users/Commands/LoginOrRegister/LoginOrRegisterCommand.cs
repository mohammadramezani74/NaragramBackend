using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Users.Commands.LoginOrRegister
{
    public sealed record LoginOrRegisterCommand(
        string phoneNumber,string verifyCode): ICommands<TokenResponse>;
}
