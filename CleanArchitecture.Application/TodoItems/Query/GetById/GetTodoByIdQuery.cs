using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Query.GetById
{
    public sealed record GetTodoByIdQuery(Guid TodoItemId):IQuery<TodoResponse>;
}
