
using System.Collections.Concurrent;

namespace CleanArchitecture.Application.Common.Models;

public class   UserDto{
    public Guid id { get; set; }
    public ConcurrentBag<string> ConnectionIds { get; set; } = new();
    public string Name { get; set; }
    public bool IsOnline { get; set; }
    public bool IsSelected { get; set; }

    public UserDto(Guid id, string name, bool Isonline = false)
    {
        this.id = id;
        Name = name;
        IsOnline = Isonline;
    }
}
