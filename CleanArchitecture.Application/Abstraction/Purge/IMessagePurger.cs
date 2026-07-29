using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Purge
{
    public interface IMessagePurger
    {
        /// <summary>پیام‌های یک گفتگو (خصوصی یا گروه) را با رعایت ترتیب کلید خارجی پاک می‌کند.</summary>
        Task<int> PurgeConversationMessagesAsync(Guid conversationId, CancellationToken ct = default);

        /// <summary>پیام‌های یک کانال را پاک می‌کند.</summary>
        Task<int> PurgeChannelMessagesAsync(Guid channelId, CancellationToken ct = default);

    }
}
