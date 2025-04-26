using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums.ToDoItems;

namespace CleanArchitecture.Domain.Entities.ToDoItems;

public sealed class TodoItem:BaseEntity
{
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Labels { get; set; } = [];
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Priority Priority { get; set; }
}
