using CleanArchitecture.Application.Common.Messaging;


namespace CleanArchitecture.Application.Authentication.Command.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken):ICommands;
