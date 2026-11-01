using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== گزارش ربات‌ها

    public sealed record BotReportSummary(
        int Bots, int EnabledBots,
        int StartedUsersTotal, int NewUsers, int ActiveUsers,
        int UserMessages, int BotMessages,
        int Runs, int DoneRuns);

    /// <summary>یک ربات در بازه. StartedUsers کل کسانی است که تا حالا شروعش کرده‌اند.</summary>
    public sealed record BotReportBot(
        Guid Id, string Name, string Handle, bool Enabled,
        int StartedUsers, int NewUsers, int ActiveUsers,
        int UserMessages, int BotMessages, int Runs, string? TopFlow);

    /// <summary>Open = هنوز تمام نشده یا کاربر وسطش رها کرده.</summary>
    public sealed record BotReportFlow(
        string Key, string Title, string BotName,
        int Runs, int Users, int Done, int Cancelled, int Failed, int Open, int? AvgSeconds);

    public sealed record BotReportUser(Guid Id, string Name, int Messages, int Runs, List<string> Bots, DateTime LastAt);

    /// <summary>اولین بار که این کاربر با این ربات کار کرده.</summary>
    public sealed record BotReportStart(Guid UserId, string Name, string BotName, DateTime FirstAt, DateTime LastAt);

    public sealed record BotReportDay(DateTime Day, int UserMessages, int ActiveUsers, int Runs);

    public sealed record BotReport(
        DateTime From, DateTime To,
        BotReportSummary Summary,
        List<BotReportBot> Bots,
        List<BotReportFlow> Flows,
        List<BotReportUser> Users,
        List<BotReportStart> NewStarts,
        List<BotReportDay> Days,
        DateTime? RunsTrackedSince);

    /// <summary>بازه‌ی روزها، از From تا To (هر دو شامل). حداکثر ۳۶۶ روز.</summary>
    public sealed record GetBotReportQuery(DateTime From, DateTime To) : IQuery<BotReport>;

    internal sealed class GetBotReportQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin, BotPartsRegistry registry)
        : IQueryHandler<GetBotReportQuery, BotReport>
    {
        private const int TopUsers = 50;
        private const int TopStarts = 50;

        public async Task<OperationResult<BotReport>> Handle(GetBotReportQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<BotReport>(AdminPanelKit.Denied());

            var from = request.From.Date;
            var to = request.To.Date;
            if (to < from) (from, to) = (to, from);
            if ((to - from).TotalDays > 366) from = to.AddDays(-366);
            var end = to.AddDays(1);

            // ---------------- ربات‌ها و گفتگوهای خصوصی هر کدام
            var bots = await uow.Bots.AsNoTracking()
                .Where(b => !b.Deleted)
                .Select(b => new { b.Id, b.UserId, Name = b.User != null ? b.User.FirsName : null, b.Handle, b.IsEnabled })
                .ToListAsync(ct);
            var botByUser = bots.ToDictionary(b => b.UserId);
            var botIds = bots.Select(b => b.Id).ToList();
            var botUserIds = bots.Select(b => b.UserId).ToList();
            string BotName(Guid botId) => bots.FirstOrDefault(b => b.Id == botId) is { } b ? (string.IsNullOrWhiteSpace(b.Name) ? "@" + b.Handle : b.Name!) : "ربات حذف‌شده";

            var botConversations = await uow.Conversation.AsNoTracking()
                .Where(c => c.IsPrivate && c.Users.Any(u => botUserIds.Contains(u.UserId)))
                .SelectMany(c => c.Users.Where(u => botUserIds.Contains(u.UserId)).Select(u => new { ConversationId = c.Id, BotUserId = u.UserId }))
                .ToListAsync(ct);
            var conversationBot = botConversations
                .GroupBy(x => x.ConversationId)
                .ToDictionary(g => g.Key, g => botByUser[g.First().BotUserId].Id);
            var conversationIds = conversationBot.Keys.ToList();

            // ---------------- پیام‌های بازه در گفتگو با ربات‌ها: هر گفتگو، فرستنده و روز یک ردیف
            var messageRows = conversationIds.Count == 0
                ? new List<MessageRow>()
                : await uow.Messages.AsNoTracking()
                    .Where(m => !m.Deleted && m.CreateDate >= from && m.CreateDate < end
                             && m.ConversationId != null && conversationIds.Contains(m.ConversationId.Value))
                    .GroupBy(m => new { ConversationId = m.ConversationId!.Value, m.CreatedByUserId, Day = m.CreateDate.Date })
                    .Select(g => new MessageRow(g.Key.ConversationId, g.Key.CreatedByUserId, g.Key.Day, g.Count(), g.Max(m => m.CreateDate)))
                    .ToListAsync(ct);

            var botUserSet = botUserIds.ToHashSet();
            var human = messageRows.Where(r => r.SenderId is Guid s && !botUserSet.Contains(s)).ToList();
            var fromBots = messageRows.Where(r => r.SenderId is Guid s && botUserSet.Contains(s)).ToList();

            // ---------------- شروع‌ها
            var sessions = await uow.BotSessions.AsNoTracking()
                .Where(s => botIds.Contains(s.BotId))
                .Select(s => new { s.BotId, s.UserId, s.CreateDate, s.StartedAt, s.LastActivityAt })
                .ToListAsync(ct);

            // ---------------- اجرای جریان‌ها
            var runs = await uow.BotFlowRuns.AsNoTracking()
                .Where(r => r.StartedAt >= from && r.StartedAt < end)
                .Select(r => new { r.BotId, r.UserId, r.FlowKey, r.Outcome, r.StartedAt, r.EndedAt })
                .ToListAsync(ct);
            var trackedSince = await uow.BotFlowRuns.AsNoTracking()
                .OrderBy(r => r.StartedAt).Select(r => (DateTime?)r.StartedAt).FirstOrDefaultAsync(ct);

            string FlowTitle(string key) => registry.FindFlow(key)?.Title ?? key;

            // ---------------- هر ربات
            var botRows = bots.Select(b =>
            {
                var mine = human.Where(r => conversationBot[r.ConversationId] == b.Id).ToList();
                var botRuns = runs.Where(r => r.BotId == b.Id).ToList();
                var top = botRuns.GroupBy(r => r.FlowKey).OrderByDescending(g => g.Count()).FirstOrDefault();
                return new BotReportBot(
                    b.Id, BotName(b.Id), b.Handle, b.IsEnabled,
                    sessions.Count(s => s.BotId == b.Id && s.StartedAt != null),
                    sessions.Count(s => s.BotId == b.Id && s.CreateDate >= from && s.CreateDate < end),
                    mine.Select(r => r.SenderId).Distinct().Count(),
                    mine.Sum(r => r.Count),
                    fromBots.Where(r => conversationBot[r.ConversationId] == b.Id).Sum(r => r.Count),
                    botRuns.Count,
                    top is null ? null : FlowTitle(top.Key));
            })
            .OrderByDescending(b => b.UserMessages + b.Runs)
            .ThenBy(b => b.Name)
            .ToList();

            // ---------------- جریان‌ها
            var staleBefore = DateTime.Now.AddHours(-24);
            var flows = runs
                .GroupBy(r => new { r.BotId, r.FlowKey })
                .Select(g =>
                {
                    var durations = g.Where(r => r.Outcome == BotFlowRun.Done && r.EndedAt != null)
                        .Select(r => (r.EndedAt!.Value - r.StartedAt).TotalSeconds).ToList();
                    return new BotReportFlow(
                        g.Key.FlowKey, FlowTitle(g.Key.FlowKey), BotName(g.Key.BotId),
                        g.Count(),
                        g.Select(r => r.UserId).Distinct().Count(),
                        g.Count(r => r.Outcome == BotFlowRun.Done),
                        g.Count(r => r.Outcome == BotFlowRun.Cancelled),
                        g.Count(r => r.Outcome is BotFlowRun.Failed or BotFlowRun.Expired),
                        g.Count(r => r.Outcome == BotFlowRun.Running),
                        durations.Count == 0 ? null : (int)Math.Round(Median(durations)));
                })
                .OrderByDescending(f => f.Runs)
                .ToList();

            // ---------------- کاربران
            var userIds = human.Select(r => r.SenderId!.Value)
                .Concat(runs.Select(r => r.UserId))
                .Distinct().ToList();
            var userStats = userIds.Select(id =>
            {
                var msgs = human.Where(r => r.SenderId == id).ToList();
                var userRuns = runs.Where(r => r.UserId == id).ToList();
                var usedBots = msgs.Select(r => conversationBot[r.ConversationId]).Concat(userRuns.Select(r => r.BotId)).Distinct().ToList();
                var last = msgs.Select(r => r.LastAt).Concat(userRuns.Select(r => r.EndedAt ?? r.StartedAt)).DefaultIfEmpty(DateTime.MinValue).Max();
                return (Id: id, Messages: msgs.Sum(r => r.Count), Runs: userRuns.Count, Bots: usedBots, Last: last);
            })
            .OrderByDescending(u => u.Messages + u.Runs * 3)
            .Take(TopUsers)
            .ToList();

            var newStarts = sessions
                .Where(s => s.CreateDate >= from && s.CreateDate < end)
                .OrderByDescending(s => s.CreateDate)
                .Take(TopStarts)
                .ToList();

            var nameIds = userStats.Select(u => u.Id).Concat(newStarts.Select(s => s.UserId)).Distinct().ToList();
            var names = await uow.Users.AsNoTracking()
                .Where(u => nameIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirsName, u.LastName })
                .ToDictionaryAsync(u => u.Id, u => AdminPanelKit.Name(u.LastName, u.FirsName), ct);
            string UserName(Guid id) => names.TryGetValue(id, out var n) ? n : "کاربر حذف‌شده";

            var users = userStats
                .Select(u => new BotReportUser(u.Id, UserName(u.Id), u.Messages, u.Runs, u.Bots.Select(BotName).ToList(), u.Last))
                .ToList();
            var starts = newStarts
                .Select(s => new BotReportStart(s.UserId, UserName(s.UserId), BotName(s.BotId), s.CreateDate, s.LastActivityAt))
                .ToList();

            // ---------------- روزها
            var days = new List<BotReportDay>();
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                var day = d;
                var dayHuman = human.Where(r => r.Day == day).ToList();
                days.Add(new BotReportDay(
                    day,
                    dayHuman.Sum(r => r.Count),
                    dayHuman.Select(r => r.SenderId).Distinct().Count(),
                    runs.Count(r => r.StartedAt.Date == day)));
            }

            var summary = new BotReportSummary(
                bots.Count, bots.Count(b => b.IsEnabled),
                sessions.Where(s => s.StartedAt != null).Select(s => s.UserId).Distinct().Count(),
                sessions.Where(s => s.CreateDate >= from && s.CreateDate < end).Select(s => s.UserId).Distinct().Count(),
                human.Select(r => r.SenderId).Distinct().Count(),
                human.Sum(r => r.Count),
                fromBots.Sum(r => r.Count),
                runs.Count,
                runs.Count(r => r.Outcome == BotFlowRun.Done));

            return OperationResult.Success(new BotReport(from, to, summary, botRows, flows, users, starts, days, trackedSince));
        }

        private sealed record MessageRow(Guid ConversationId, Guid? SenderId, DateTime Day, int Count, DateTime LastAt);

        private static double Median(List<double> values)
        {
            values.Sort();
            var mid = values.Count / 2;
            return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2;
        }
    }
}
