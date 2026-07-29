using CleanArchitecture.Application.Abstraction.Purge;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Purge
{
   
        internal sealed class MessagePurger(IApplicationUnitOfWork uow) : IMessagePurger
        {
            private readonly IApplicationUnitOfWork _uow = uow;

            public Task<int> PurgeConversationMessagesAsync(Guid conversationId, CancellationToken ct = default)
                => PurgeAsync(m => m.ConversationId == conversationId, ct);

            public Task<int> PurgeChannelMessagesAsync(Guid channelId, CancellationToken ct = default)
                => PurgeAsync(m => m.ChannelId == channelId, ct);

            private async Task<int> PurgeAsync(
                System.Linq.Expressions.Expression<Func<Domain.Entities.Chat.Message, bool>> scope,
                CancellationToken ct)
            {
                var messageIds = await _uow.Messages
                    .AsNoTracking()
                    .Where(scope)
                    .Select(m => m.Id)
                    .ToListAsync(ct);

                if (messageIds.Count == 0) return 0;

                // برای مجموعه‌های خیلی بزرگ، Contains با لیست چندهزارتایی کوئری را
                // کند می‌کند. تکه‌تکه پیش می‌رویم.
                const int BatchSize = 2000;

                foreach (var batch in Chunk(messageIds, BatchSize))
                {
                    // ۱ — واکنش‌ها (Restrict)
                    await _uow.MessageReactions
                        .Where(r => batch.Contains(r.MessageId))
                        .ExecuteDeleteAsync(ct);

                    // ۲ — فایل‌ها (Restrict)
                    await _uow.ChatFiles
                        .Where(f => batch.Contains(f.MessageId))
                        .ExecuteDeleteAsync(ct);

                    // ۳ — نشان «دیده شد» کانال (Cascade است ولی صریح مطمئن‌تر)
                    await _uow.ChannelMessageSeens
                        .Where(s => batch.Contains(s.MessageId))
                        .ExecuteDeleteAsync(ct);

                    // ۴ — شکستن ارجاع خودارجاع ریپلای.
                    // ParentMessageId رابطه‌ی Restrict به خود Message دارد، پس اگر
                    // پیامی والدِ پیام دیگری باشد حذفش رد می‌شود. این شامل ریپلای‌های
                    // خارج از دسته هم می‌شود، برای همین شرط روی ParentMessageId است
                    // نه روی Id.
                    await _uow.Messages
                        .Where(m => m.ParentMessageId != null && batch.Contains(m.ParentMessageId.Value))
                        .ExecuteUpdateAsync(s => s.SetProperty(m => m.ParentMessageId, (Guid?)null), ct);
                }

                // ۵ — خود پیام‌ها
                var deleted = 0;
                foreach (var batch in Chunk(messageIds, BatchSize))
                {
                    deleted += await _uow.Messages
                        .Where(m => batch.Contains(m.Id))
                        .ExecuteDeleteAsync(ct);
                }

                return deleted;
            }

            private static IEnumerable<List<T>> Chunk<T>(List<T> source, int size)
            {
                for (var i = 0; i < source.Count; i += size)
                    yield return source.GetRange(i, Math.Min(size, source.Count - i));
            }
        }
}
