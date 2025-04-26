using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Domain.Enums.ToDoItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Command.Create
{
    public sealed class CreateTodoCommand:ICommands<Guid>
    {
        public string Description { get; set; } = null!;
        public DateTime? DueDate { get; set; }
        public List<string> Labels { get; set; } = [];
        public Priority Priority { get; set; }
    }
}
