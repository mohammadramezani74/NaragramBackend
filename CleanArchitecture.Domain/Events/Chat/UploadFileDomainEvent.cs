using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Events.Chat
{
    public sealed record UploadFileDomainEvent(Guid ConversationId):IDomainEvent;
   
}
