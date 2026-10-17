using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== گزارش استفاده

    public sealed record UsageDay(DateTime Day, int Private, int Group, int Channel, int ActiveSenders, int BotMessages, int NewGroups, int NewChannels);

    public sealed record UsageReport(
        DateTime From, DateTime To,
        int TotalMessages, int TotalPrivate, int TotalGroup, int TotalChannel,
        int ActiveSendersTotal, int AvgActiveSenders, int BotMessages, int NewGroups, int NewChannels,
        List<UsageDay> Days, List<int> ByHour, List<int> ByWeekday, List<DashboardSlice> ByCity);

    /// <summary>بازه‌ی روزها، از From تا To (هر دو شامل). حداکثر ۳۶۶ روز.</summary>
    public sealed record GetUsageReportQuery(DateTime From, DateTime To) : IQuery<UsageReport>;

    internal sealed class GetUsageReportQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetUsageReportQuery, UsageReport>
    {
        public async Task<OperationResult<UsageReport>> Handle(GetUsageReportQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<UsageReport>(AdminPanelKit.Denied());

            var from = request.From.Date;
            var to = request.To.Date;
            if (to < from) (from, to) = (to, from);
            if ((to - from).TotalDays > 366) from = to.AddDays(-366);
            var end = to.AddDays(1);

            var messages = uow.Messages.AsNoTracking().Where(m => !m.Deleted && m.CreateDate >= from && m.CreateDate < end);

            var kindRows = await messages
                .Select(m => new { Day = m.CreateDate.Date, Kind = m.ChannelId != null ? 2 : (m.Conversation != null && m.Conversation.IsPrivate ? 0 : 1) })
                .GroupBy(x => new { x.Day, x.Kind })
                .Select(g => new { g.Key.Day, g.Key.Kind, Count = g.Count() })
                .ToListAsync(ct);

            // فرستنده‌ی فعال = کاربر واقعی (نه ربات) که آن روز دست‌کم یک پیام فرستاده.
            var humanMessages = messages.Where(m => m.CreatedByUserId != null && m.CreatedByUser != null && !m.CreatedByUser.IsBot);
            var senderRows = await humanMessages
                .GroupBy(m => m.CreateDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Select(x => x.CreatedByUserId).Distinct().Count() })
                .ToListAsync(ct);
            var sendersTotal = await humanMessages.Select(m => m.CreatedByUserId).Distinct().CountAsync(ct);

            var botRows = await messages
                .Where(m => m.Conversation != null && m.Conversation.IsPrivate && m.Conversation.Users.Any(cu => cu.User.IsBot))
                .GroupBy(m => m.CreateDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var groupRows = await uow.Conversation.AsNoTracking()
                .Where(c => !c.IsPrivate && !c.Deleted && c.CreateDate >= from && c.CreateDate < end)
                .GroupBy(c => c.CreateDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            var channelRows = await uow.Channels.AsNoTracking()
                .Where(c => !c.Deleted && c.CreateDate >= from && c.CreateDate < end)
                .GroupBy(c => c.CreateDate.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var hourRows = await messages
                .GroupBy(m => m.CreateDate.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var cityRows = await (
                    from m in humanMessages
                    join u in uow.Users.AsNoTracking() on m.CreatedByUserId equals u.Id
                    group m by u.Address!.City into g
                    select new { City = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var days = new List<UsageDay>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var day = d;
                days.Add(new UsageDay(
                    day,
                    kindRows.Where(r => r.Day == day && r.Kind == 0).Sum(r => r.Count),
                    kindRows.Where(r => r.Day == day && r.Kind == 1).Sum(r => r.Count),
                    kindRows.Where(r => r.Day == day && r.Kind == 2).Sum(r => r.Count),
                    senderRows.FirstOrDefault(r => r.Day == day)?.Count ?? 0,
                    botRows.FirstOrDefault(r => r.Day == day)?.Count ?? 0,
                    groupRows.FirstOrDefault(r => r.Day == day)?.Count ?? 0,
                    channelRows.FirstOrDefault(r => r.Day == day)?.Count ?? 0));
            }

            // روز هفته: شنبه اول (DayOfWeek.Saturday = 6)
            var byWeekday = Enumerable.Range(0, 7)
                .Select(i => days.Where(x => ((int)x.Day.DayOfWeek + 1) % 7 == i).Sum(x => x.Private + x.Group + x.Channel))
                .ToList();

            var byCity = cityRows
                .Select(r => new DashboardSlice(string.IsNullOrWhiteSpace(r.City) ? "نامشخص" : r.City.Trim(), r.Count))
                .GroupBy(x => x.Label)
                .Select(g => new DashboardSlice(g.Key, g.Sum(x => x.Count)))
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToList();

            var tp = days.Sum(x => x.Private);
            var tg = days.Sum(x => x.Group);
            var tc = days.Sum(x => x.Channel);
            return OperationResult.Success(new UsageReport(
                from, to, tp + tg + tc, tp, tg, tc,
                sendersTotal,
                days.Count == 0 ? 0 : (int)Math.Round(days.Average(x => x.ActiveSenders)),
                days.Sum(x => x.BotMessages), days.Sum(x => x.NewGroups), days.Sum(x => x.NewChannels),
                days,
                Enumerable.Range(0, 24).Select(h => hourRows.FirstOrDefault(r => r.Hour == h)?.Count ?? 0).ToList(),
                byWeekday,
                byCity));
        }
    }

    // ================================================================== کم‌فعال‌ها

    public sealed record InactiveUserRow(Guid Id, string Name, string? City, string PhoneMasked, DateTime? LastSeen, DateTime? CreatedAt);

    public sealed record InactiveUsersPage(List<InactiveUserRow> Items, int Total, int Days);

    /// <summary>کاربرهای فعال (غیرفعال‌نشده) که Days روز یا بیشتر آنلاین نبوده‌اند؛ قدیمی‌ترها اول.</summary>
    public sealed record GetInactiveUsersQuery(int Days, int Take) : IQuery<InactiveUsersPage>;

    internal sealed class GetInactiveUsersQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetInactiveUsersQuery, InactiveUsersPage>
    {
        public async Task<OperationResult<InactiveUsersPage>> Handle(GetInactiveUsersQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<InactiveUsersPage>(AdminPanelKit.Denied());

            var days = Math.Clamp(request.Days, 7, 365);
            var since = DateTimeOffset.Now.AddDays(-days);
            var q = uow.Users.AsNoTracking()
                .Where(u => !u.IsBot && !u.IsDisabled && (u.LastLoginDate == null || u.LastLoginDate < since));

            var total = await q.CountAsync(ct);
            var rows = await q
                .OrderBy(u => u.LastLoginDate)
                .Take(Math.Clamp(request.Take, 10, 2000))
                .Select(u => new { u.Id, u.FirsName, u.LastName, City = u.Address!.City, u.PhoneNumber, u.LastLoginDate, u.CreatedAt })
                .ToListAsync(ct);

            return OperationResult.Success(new InactiveUsersPage(
                rows.Select(r => new InactiveUserRow(
                    r.Id, AdminPanelKit.Name(r.LastName, r.FirsName),
                    string.IsNullOrWhiteSpace(r.City) ? null : r.City,
                    AdminPanelKit.MaskPhone(r.PhoneNumber),
                    r.LastLoginDate?.LocalDateTime, r.CreatedAt?.LocalDateTime)).ToList(),
                total, days));
        }
    }

    // ================================================================== رویدادهای پنل

    public sealed record AuditRow(DateTime At, Guid ActorId, string ActorName, string Action, string Text, string TargetType, Guid? TargetId, string TargetName, string? Details);

    public sealed record AuditActor(Guid Id, string Name);

    public sealed record AuditPage(List<AuditRow> Items, int Total, int Page, int PageSize, List<AuditActor> Actors);

    /// <summary>Area: all | user | room | role | broadcast</summary>
    public sealed record GetAuditLogQuery(int Page, int PageSize, string? Area, Guid? ActorId, DateTime? From, DateTime? To, string? Search) : IQuery<AuditPage>;

    internal sealed class GetAuditLogQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetAuditLogQuery, AuditPage>
    {
        public async Task<OperationResult<AuditPage>> Handle(GetAuditLogQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<AuditPage>(AdminPanelKit.Denied());

            var page = Math.Max(1, request.Page);
            var size = Math.Clamp(request.PageSize, 10, 2000);
            var q = uow.AdminAuditLogs.AsNoTracking();

            if (request.Area is "user" or "room" or "role" or "broadcast")
            {
                var prefix = request.Area + ".";
                q = q.Where(a => a.Action.StartsWith(prefix));
            }
            if (request.ActorId is Guid actor)
                q = q.Where(a => a.ActorId == actor);
            if (request.From is DateTime f)
                q = q.Where(a => a.CreateDate >= f.Date);
            if (request.To is DateTime t)
            {
                var until = t.Date.AddDays(1);
                q = q.Where(a => a.CreateDate < until);
            }
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(a => a.TargetName.Contains(s) || (a.Details != null && a.Details.Contains(s)) || a.ActorName.Contains(s));
            }

            var total = await q.CountAsync(ct);
            var rows = await q.OrderByDescending(a => a.CreateDate)
                .Skip((page - 1) * size).Take(size)
                .Select(a => new { a.CreateDate, a.ActorId, a.ActorName, a.Action, a.TargetType, a.TargetId, a.TargetName, a.Details })
                .ToListAsync(ct);

            var actors = await uow.AdminAuditLogs.AsNoTracking()
                .GroupBy(a => a.ActorId)
                .Select(g => new { Id = g.Key, Name = g.Max(x => x.ActorName) })
                .ToListAsync(ct);

            return OperationResult.Success(new AuditPage(
                rows.Select(a => new AuditRow(a.CreateDate, a.ActorId, a.ActorName, a.Action, AdminPanelKit.ActionText(a.Action),
                    a.TargetType, a.TargetId, a.TargetName, a.Details)).ToList(),
                total, page, size,
                actors.Select(a => new AuditActor(a.Id, a.Name)).OrderBy(a => a.Name).ToList()));
        }
    }
}
