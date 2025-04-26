using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Hubs.Models
{
    public record MessageSeenDto(List<Guid> messageId, Guid UserId);
}
