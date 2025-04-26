using CleanArchitecture.Domain.Events.todoItemEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.TodoItems.Command.Create;

internal sealed class CreateTodoDomainEventHandler(ILogger<CreateTodoDomainEventHandler> logger) : INotificationHandler<TodoItemCreatedDomainEvent>
{
    private readonly ILogger<CreateTodoDomainEventHandler> _logger = logger;

    public  Task Handle(TodoItemCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Send an Email For TodoItems{dateTime}",DateTime.Now);
        return Task.CompletedTask;
    }
}
