using CleanArchitecture.Application.Common.Utilities.Extensions.DateExtensions;
using CleanArchitecture.Domain.Entities.ToDoItems;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.TodoItems.Query.Get
{
    public sealed class TodoResponse : IRegister
    {
       
            public Guid Id { get; set; }
            public Guid UserId { get; set; }
            public string Description { get; set; }
            public DateTime? DueDate { get; set; }
            public List<string> Labels { get; set; }
            public bool IsCompleted { get; set; }
            public string CreatedAt { get; set; }
            public string? CompletedAt { get; set; }

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<TodoItem, TodoResponse>()
                .Map(des => des.CreatedAt, src => src.CreateDate.ToFarsi())
                .Map(des=>des.CompletedAt,src=>src.CompletedAt.ToFarsi())
                .Map(des=>des.UserId,src=>src.CreatedByUserId);
        }
    }
}
