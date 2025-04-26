using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CleanArchitecture.Application.TodoItems.Query.GetById
{
    internal sealed class GetTodoByIdQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<GetTodoByIdQuery, TodoResponse>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<TodoResponse>> Handle(GetTodoByIdQuery request, CancellationToken cancellationToken)
        {
            
            var todo = await _uow.TodoItem
           .Where(todoItem => todoItem.Id == request.TodoItemId)
           .Select(todoItem => new TodoResponse
           {
               Id = todoItem.Id,
               UserId = todoItem.CreatedByUserId!.Value,
               Description = todoItem.Description??string.Empty,
               DueDate = todoItem.DueDate,
               Labels = todoItem.Labels,
               IsCompleted = todoItem.IsCompleted,
               CreatedAt = todoItem.CreateDate,
               CompletedAt = todoItem.CompletedAt
           })
           .SingleOrDefaultAsync(cancellationToken);

            if (todo is null)
            {
                return OperationResult.Failure<TodoResponse>(new OperationResult().Failed("آیتم مورد نظر یافت نشد"));
            }

            return todo;
        }
    }
}
