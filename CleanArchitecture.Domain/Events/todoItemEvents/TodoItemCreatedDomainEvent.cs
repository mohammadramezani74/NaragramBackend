using SharedKernel;

namespace CleanArchitecture.Domain.Events.todoItemEvents;

public sealed record TodoItemCreatedDomainEvent(Guid TodoItemId) : IDomainEvent;
