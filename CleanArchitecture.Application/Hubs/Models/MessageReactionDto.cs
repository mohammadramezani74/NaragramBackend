using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Hubs.Models
{
    public sealed record MessageReactionDto(Guid MessageId, string? Reaction, Guid OtherUserId);

}
