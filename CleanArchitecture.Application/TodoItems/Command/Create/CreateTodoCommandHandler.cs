using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Entities.ToDoItems;
using CleanArchitecture.Domain.Events.todoItemEvents;
using SharedKernel;

namespace CleanArchitecture.Application.TodoItems.Command.Create
{
    internal sealed class CreateTodoCommandHandler(IApplicationUnitOfWork uow
        ,IDateTimeProvider dateTime,
        IApplicationUserManager userManager)
        : ICommandHandler<CreateTodoCommand, Guid>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IDateTimeProvider _dateTime = dateTime;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult<Guid>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
        {
            User? user = await _userManager.GetUserBy (_userManager.UserId!.Value);

            if (user is null)
            {
                return OperationResult.Failure<Guid>(new OperationResult().Failed("User Not Found"));
            }

            var todoItem = new TodoItem
            {
                CreatedByUserId = user.Id,
                Description = request.Description,
                Priority = request.Priority,
                DueDate = request.DueDate,
                Labels = request.Labels,
                IsCompleted = false,
                CreateDate = _dateTime.Now,
            };

            todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

            _uow.TodoItem.Add(todoItem);

            await _uow.SaveChangesAsync(cancellationToken);

            return todoItem.Id;
        }
    }
}
