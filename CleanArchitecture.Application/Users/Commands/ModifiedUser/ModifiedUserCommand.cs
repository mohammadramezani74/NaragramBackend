using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Users.Commands.ModifiedUser
{
    public sealed record ModifiedUserCommand(string?phoneNumber,
        string? bio,
        string?Email,
        string? City
        ):ICommands;

}
