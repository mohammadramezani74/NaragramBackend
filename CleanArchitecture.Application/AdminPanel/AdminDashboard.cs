using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Caching;
using CleanArchitecture.Application.Chats.Hubs.Services;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== DTOها

    public sealed record AdminDashboardDto(
        DashboardKpis Kpis,
        List<DashboardDayPoint> Trend,
        List<DashboardSlice> UsersByCity,
        DashboardActiveSplit Activity7Days,
        List<DashboardSlice> MessageTypes,
        List<DashboardTopItem> TopUsers,
        List<DashboardTopItem> TopGroups,
        List<DashboardTopItem> TopChannels,
        List<DashboardActivity> Recent,
        DashboardBots Bots,
        DashboardOnline Online,
        DateTime GeneratedAt);

    public sealed record DashboardKpis(
        int Messages24h, int MessagesPrev24h,
        int Users, int NewUsersWeek,
        int Groups, int NewGroupsWeek,
        int Channels, int NewChannelsWeek);

    /// <summary>پیام‌های یک روز، جدا برای خصوصی، گروه و کانال.</summary>
    public sealed record DashboardDayPoint(DateTime Day, int Private, int Group, int Channel);

    public sealed record DashboardSlice(string Label, int Count);

    public sealed record DashboardActiveSplit(int Active, int Inactive);

    public sealed record DashboardTopItem(Guid Id, string Name, int Count);

    /// <summary>Kind: user | group | channel | bot</summary>
    public sealed record DashboardActivity(string Kind, string Title, string Detail, DateTime At);

    public sealed record DashboardBots(int Count, int Enabled, int StartedUsers, int MessagesToday);

    public sealed record DashboardOnline(int Count, List<DashboardOnlineUser> Users);

    public sealed record DashboardOnlineUser(Guid Id, string Name);

    // ================================================================== کوئری

    /// <summary>آمار داشبورد پنل ادمین. Days: ۷ یا ۳۰ (بازه‌ی نمودار روند).</summary>
    public sealed record GetAdminDashboardQuery(int Days) : IQuery<AdminDashboardDto>;

    internal sealed class GetAdminDashboardQueryHandler(
        IApplicationUnitOfWork uow,
        IAdminAccess admin,
        ICacheService cache,
        UserOnlineService online)
        : IQueryHandler<GetAdminDashboardQuery, AdminDashboardDto>
    {
        // آمار سنگین (روی جدول بزرگ پیام‌ها) یک دقیقه کش می‌شود؛ آنلاین‌ها همیشه تازه‌اند.
        private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(1);

        public async Task<OperationResult<AdminDashboardDto>> Handle(GetAdminDashboardQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AdminDashboardDto>(new OperationResult().NotFound("یافت نشد."));

            var days = request.Days >= 30 ? 30 : 7;
            var stats = await cache.GetOrCreateAsync($"admin-dashboard:{days}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheFor;
                return await BuildAsync(days, ct);
            });

            return OperationResult.Success(stats with { Online = await OnlineAsync(ct) });
        }

        private async Task<AdminDashboardDto> BuildAsync(int days, CancellationToken ct)
        {
            var now = DateTime.Now; // CreateDate پیام‌ها با DateTime.Now ذخیره می‌شود
            var today = now.Date;
            var weekAgo = now.AddDays(-7);
            var weekAgoOffset = DateTimeOffset.Now.AddDays(-7);

            var messages = uow.Messages.AsNoTracking().Where(m => !m.Deleted);
            var users = uow.Users.AsNoTracking().Where(u => !u.IsBot);
            var groups = uow.Conversation.AsNoTracking().Where(c => !c.IsPrivate && !c.Deleted);
            var channels = uow.Channels.AsNoTracking().Where(c => !c.Deleted);

            // ---------------- کارت‌ها
            var dayAgo = now.AddHours(-24);
            var twoDaysAgo = now.AddHours(-48);
            var kpis = new DashboardKpis(
                await messages.CountAsync(m => m.CreateDate >= dayAgo, ct),
                await messages.CountAsync(m => m.CreateDate >= twoDaysAgo && m.CreateDate < dayAgo, ct),
                await users.CountAsync(ct),
                await users.CountAsync(u => u.CreatedAt != null && u.CreatedAt >= weekAgoOffset, ct),
                await groups.CountAsync(ct),
                await groups.CountAsync(c => c.CreateDate >= weekAgo, ct),
                await channels.CountAsync(ct),
                await channels.CountAsync(c => c.CreateDate >= weekAgo, ct));

            // ---------------- روند: پیام در روز، به تفکیک نوع گفتگو
            var since = today.AddDays(-(days - 1));
            var rows = await messages
                .Where(m => m.CreateDate >= since)
                .Select(m => new
                {
                    Day = m.CreateDate.Date,
                    Kind = m.ChannelId != null ? 2 : (m.Conversation != null && m.Conversation.IsPrivate ? 0 : 1)
                })
                .GroupBy(x => new { x.Day, x.Kind })
                .Select(g => new { g.Key.Day, g.Key.Kind, Count = g.Count() })
                .ToListAsync(ct);

            var trend = Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .Select(d => new DashboardDayPoint(
                    d,
                    rows.Where(r => r.Day == d && r.Kind == 0).Sum(r => r.Count),
                    rows.Where(r => r.Day == d && r.Kind == 1).Sum(r => r.Count),
                    rows.Where(r => r.Day == d && r.Kind == 2).Sum(r => r.Count)))
                .ToList();

            // ---------------- کاربران بر اساس شهر (از INTS) و فعالیت
            var cityRows = await users
                .GroupBy(u => u.Address!.City)
                .Select(g => new { City = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var cities = cityRows
                .Select(x => new DashboardSlice(string.IsNullOrWhiteSpace(x.City) ? "نامشخص" : x.City.Trim(), x.Count))
                .GroupBy(x => x.Label)
                .Select(g => new DashboardSlice(g.Key, g.Sum(x => x.Count)))
                .OrderByDescending(x => x.Count)
                .ToList();
            var usersByCity = cities.Take(4).ToList();
            var otherCities = cities.Skip(4).Sum(x => x.Count);
            if (otherCities > 0)
                usersByCity.Add(new DashboardSlice("سایر شهرها", otherCities));

            var active = await users.CountAsync(u => u.LastLoginDate != null && u.LastLoginDate >= weekAgoOffset, ct);
            var activity7 = new DashboardActiveSplit(active, Math.Max(0, kpis.Users - active));

            // ---------------- نوع پیام‌ها در ۷ روز
            var typeRows = await messages
                .Where(m => m.CreateDate >= weekAgo)
                .GroupBy(m => m.MessageType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var messageTypes = Enum.GetValues<MessageType>()
                .Select(t => new DashboardSlice(TypeLabel(t), typeRows.Where(r => r.Type == t).Sum(r => r.Count)))
                .OrderByDescending(x => x.Count)
                .ToList();

            // ---------------- فعال‌ترین‌ها در ۷ روز
            var weekMessages = messages.Where(m => m.CreateDate >= weekAgo);

            var senderRows = await weekMessages
                .Where(m => m.CreatedByUserId != null)
                .GroupBy(m => m.CreatedByUserId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(12)
                .ToListAsync(ct);
            var senderIds = senderRows.Select(x => x.Id).ToList();
            var senderNames = await uow.Users.AsNoTracking()
                .Where(u => senderIds.Contains(u.Id) && !u.IsBot)
                .Select(u => new { u.Id, u.FirsName, u.LastName })
                .ToListAsync(ct);
            var topUsers = senderRows
                .Select(r => (Row: r, User: senderNames.FirstOrDefault(u => u.Id == r.Id)))
                .Where(x => x.User != null)
                .Take(5)
                .Select(x => new DashboardTopItem(x.Row.Id, DisplayName(x.User!.LastName, x.User.FirsName), x.Row.Count))
                .ToList();

            var groupRows = await weekMessages
                .Where(m => m.ConversationId != null && m.Conversation != null && !m.Conversation.IsPrivate)
                .GroupBy(m => m.ConversationId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync(ct);
            var groupIds = groupRows.Select(x => x.Id).ToList();
            var groupTitles = await uow.Conversation.AsNoTracking()
                .Where(c => groupIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Title })
                .ToListAsync(ct);
            var topGroups = groupRows
                .Select(r => new DashboardTopItem(r.Id, groupTitles.FirstOrDefault(t => t.Id == r.Id)?.Title ?? "گروه", r.Count))
                .ToList();

            var channelRows = await weekMessages
                .Where(m => m.ChannelId != null)
                .GroupBy(m => m.ChannelId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync(ct);
            var channelIds = channelRows.Select(x => x.Id).ToList();
            var channelTitles = await uow.Channels.AsNoTracking()
                .Where(c => channelIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Title })
                .ToListAsync(ct);
            var topChannels = channelRows
                .Select(r => new DashboardTopItem(r.Id, channelTitles.FirstOrDefault(t => t.Id == r.Id)?.Title ?? "کانال", r.Count))
                .ToList();

            // ---------------- فعالیت‌های اخیر
            var recent = new List<DashboardActivity>();

            var newUsers = await users
                .Where(u => u.CreatedAt != null)
                .OrderByDescending(u => u.CreatedAt)
                .Take(6)
                .Select(u => new { u.FirsName, u.LastName, City = u.Address!.City, u.CreatedAt })
                .ToListAsync(ct);
            recent.AddRange(newUsers.Select(u => new DashboardActivity(
                "user", "کاربر تازه وارد شد",
                DisplayName(u.LastName, u.FirsName) + (string.IsNullOrWhiteSpace(u.City) ? string.Empty : " · " + u.City),
                u.CreatedAt!.Value.LocalDateTime)));

            var newGroups = await groups
                .OrderByDescending(c => c.CreateDate)
                .Take(6)
                .Select(c => new { c.Title, Members = c.Users.Count, c.CreateDate })
                .ToListAsync(ct);
            recent.AddRange(newGroups.Select(g => new DashboardActivity(
                "group", "گروه تازه ساخته شد", $"{g.Title} · {g.Members} عضو", g.CreateDate)));

            var newChannels = await channels
                .OrderByDescending(c => c.CreateDate)
                .Take(6)
                .Select(c => new { c.Title, c.CreateDate })
                .ToListAsync(ct);
            recent.AddRange(newChannels.Select(c => new DashboardActivity(
                "channel", "کانال تازه ساخته شد", c.Title, c.CreateDate)));

            var newBots = await uow.Bots.AsNoTracking()
                .Where(b => !b.Deleted)
                .OrderByDescending(b => b.CreateDate)
                .Take(6)
                .Select(b => new { Name = b.User != null ? b.User.FirsName : b.Handle, b.Handle, b.CreateDate })
                .ToListAsync(ct);
            recent.AddRange(newBots.Select(b => new DashboardActivity(
                "bot", "ربات تازه ساخته شد", $"{b.Name} · @{b.Handle}", b.CreateDate)));

            recent = recent.OrderByDescending(x => x.At).Take(8).ToList();

            // ---------------- ربات‌ها
            var bots = new DashboardBots(
                await uow.Bots.CountAsync(b => !b.Deleted, ct),
                await uow.Bots.CountAsync(b => !b.Deleted && b.IsEnabled, ct),
                await uow.BotSessions.CountAsync(s => s.StartedAt != null, ct),
                await messages.CountAsync(m => m.CreateDate >= today
                    && m.Conversation != null
                    && m.Conversation.IsPrivate
                    && m.Conversation.Users.Any(cu => cu.User.IsBot), ct));

            return new AdminDashboardDto(
                kpis, trend, usersByCity, activity7, messageTypes,
                topUsers, topGroups, topChannels, recent, bots,
                new DashboardOnline(0, new List<DashboardOnlineUser>()),
                now);
        }

        private async Task<DashboardOnline> OnlineAsync(CancellationToken ct)
        {
            var ids = online.OnlineUsers.Values.Select(x => x.UserId).Distinct().ToList();
            if (ids.Count == 0)
                return new DashboardOnline(0, new List<DashboardOnlineUser>());

            var people = await uow.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id) && !u.IsBot)
                .OrderBy(u => u.LastName)
                .Take(8)
                .Select(u => new { u.Id, u.FirsName, u.LastName })
                .ToListAsync(ct);

            return new DashboardOnline(
                ids.Count,
                people.Select(p => new DashboardOnlineUser(p.Id, DisplayName(p.LastName, p.FirsName))).ToList());
        }

        // همان قرار بقیه‌ی برنامه: «LastName FirsName»
        private static string DisplayName(string? lastName, string? firstName)
            => string.Join(" ", new[] { lastName, firstName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

        private static string TypeLabel(MessageType type) => type switch
        {
            MessageType.Text => "متن",
            MessageType.Image => "عکس",
            MessageType.Document => "فایل",
            MessageType.Audio => "صدا",
            MessageType.Video => "ویدیو",
            MessageType.Location => "موقعیت",
            _ => type.ToString()
        };
    }
}
