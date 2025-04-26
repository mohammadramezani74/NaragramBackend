using CleanArchitecture.Application.Common.Messaging;

namespace CleanArchitecture.Application.TodoItems.Command.Delete
{
    public sealed record DeleteTodoCommand(Guid TodoItemId):ICommands;
 
}
