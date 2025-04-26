using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Outbox
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; } =null!;
        public string Content { get; set; } = null!;
        public DateTime Occured { get; set; }
        public DateTime? Processed { get; set; }
        public string? Error { get; set; }
    }
}
