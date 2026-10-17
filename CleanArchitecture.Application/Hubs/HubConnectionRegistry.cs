using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace CleanArchitecture.Application.Hubs
{
    /// <summary>
    /// اتصال‌های باز هاب، برای این‌که وقتی حساب کسی غیرفعال شد یا از همه‌ی دستگاه‌ها خارج شد،
    /// اتصال زنده‌اش همان لحظه بسته شود (IHubContext نمی‌تواند اتصال را قطع کند).
    /// </summary>
    public sealed class HubConnectionRegistry
    {
        private readonly ConcurrentDictionary<string, (Guid UserId, HubCallerContext Context)> _connections = new();

        public void Add(Guid userId, HubCallerContext context)
            => _connections[context.ConnectionId] = (userId, context);

        public void Remove(string connectionId)
            => _connections.TryRemove(connectionId, out _);

        /// <summary>شناسه‌ی اتصال‌های باز این کاربر (همه‌ی تب‌ها و دستگاه‌ها).</summary>
        public IReadOnlyList<string> ConnectionsOf(Guid userId)
            => _connections.Where(c => c.Value.UserId == userId).Select(c => c.Key).ToList();

        /// <summary>همه‌ی اتصال‌های این کاربر را می‌بندد؛ تعداد بسته‌شده را برمی‌گرداند.</summary>
        public int Abort(Guid userId)
        {
            var count = 0;
            foreach (var (id, entry) in _connections)
            {
                if (entry.UserId != userId)
                    continue;

                try
                {
                    entry.Context.Abort();
                    count++;
                }
                catch (Exception)
                {
                    // اتصال در همین لحظه بسته شده بود.
                }

                _connections.TryRemove(id, out _);
            }
            return count;
        }
    }
}
