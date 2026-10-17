using CleanArchitecture.Application.Hubs.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Hubs
{
    /// <summary>
    /// عضویت اتصال‌های زنده در گروه‌های SignalR (نام گروه = شناسه‌ی گروه یا کانال).
    /// هاب فقط موقع وصل شدن، اتصال را به گروه‌ها و کانال‌های کاربر اضافه می‌کند. بدون این کلاس
    /// عضو تازه تا اتصال بعدی پیام زنده نمی‌گرفت و عضو بیرون‌شده تا اتصال بعدی هنوز می‌گرفت.
    /// فقط بعد از ذخیره‌ی موفق در دیتابیس صدا زده شود.
    /// </summary>
    public sealed class HubRoomMembership(HubConnectionRegistry connections,
        IHubContext<NaraHub, IChatHubClient> hub, ILogger<HubRoomMembership> logger)
    {
        public Task AddAsync(Guid roomId, IEnumerable<Guid> userIds)
            => ApplyAsync(roomId, userIds, add: true);

        public Task RemoveAsync(Guid roomId, IEnumerable<Guid> userIds)
            => ApplyAsync(roomId, userIds, add: false);

        /// <summary>
        /// عضو از گروه یا کانال بیرون رفت: اتصال‌هایش از گروه SignalR برداشته می‌شوند و
        /// همه‌ی دستگاه‌هایش ردیف آن گفتگو را برمی‌دارند (GetDeletedChannel در فرانت
        /// برای گروه و کانال هر دو کار می‌کند).
        /// </summary>
        public async Task RemovedAsync(Guid roomId, Guid userId)
        {
            await ApplyAsync(roomId, new[] { userId }, add: false);
            try
            {
                await hub.Clients.User(userId.ToString()).GetDeletedChannel(roomId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not notify user {UserId} about removal from {RoomId}", userId, roomId);
            }
        }

        private async Task ApplyAsync(Guid roomId, IEnumerable<Guid> userIds, bool add)
        {
            var group = roomId.ToString();
            foreach (var userId in userIds.Distinct())
            {
                foreach (var connectionId in connections.ConnectionsOf(userId))
                {
                    try
                    {
                        if (add)
                            await hub.Groups.AddToGroupAsync(connectionId, group);
                        else
                            await hub.Groups.RemoveFromGroupAsync(connectionId, group);
                    }
                    catch (Exception ex)
                    {
                        // تغییر در دیتابیس ذخیره شده؛ اگر این‌جا نشد، با اتصال بعدی درست می‌شود.
                        logger.LogWarning(ex, "SignalR group update failed ({Action}) for {RoomId}", add ? "add" : "remove", roomId);
                    }
                }
            }
        }
    }
}
