using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Query.Get
{
    public sealed class GetTodosQuery() : IQuery<TodoResponse[]>;

}
