using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.Roles.Commands.DeleteRole;

public record DeleteRoleCommand(string Name):ICommands;

