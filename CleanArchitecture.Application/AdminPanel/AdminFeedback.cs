using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots;
using CleanArchitecture.Application.Bots.Commands;
using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Feedback;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Admin;
using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== بازخورد کاربران (نظر و پیشنهاد درباره‌ی اپ)

    public sealed record FeedbackRow(
        Guid Id, int Number, string Kind, string Status, string Snippet,
        Guid UserId, string UserName, DateTime CreatedAt, bool HasReply);

    public sealed record FeedbackCounts(int All, int New, int Seen, int Doing, int Done, int Rejected);

    public sealed record FeedbackPage(List<FeedbackRow> Items, int Total, int Page, int PageSize, FeedbackCounts Counts);

    public sealed record FeedbackDetail(
        Guid Id, int Number, string Kind, string Status, string Text,
        Guid UserId, string UserName, string? City, string Phone,
        DateTime CreatedAt, DateTime? UpdatedAt,
        string? Reply, DateTime? RepliedAt, string? RepliedByName,
        bool CanReply, int UserFeedbackCount);

    internal static class FeedbackKit
    {
        public const int MaxScan = 5000;

        /// <summary>
        /// پیامی از ربات پشتیبانی به کاربر (بیرون از نوبت ربات، مثل خلاصه‌ی صبحگاهی).
        /// اگر ربات یا گفتگو نباشد false برمی‌گرداند.
        /// </summary>
        public static async Task<bool> SendAsync(
            IApplicationUnitOfWork uow, BotMessenger messenger, AppFeedback feedback, string text, CancellationToken ct)
        {
            if (feedback.BotId is not Guid botId)
                return false;

            var bot = await uow.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == botId && !b.Deleted, ct);
            var human = await uow.Users.FirstOrDefaultAsync(u => u.Id == feedback.UserId, ct);
            if (bot?.User is null || human is null || human.IsDisabled)
                return false;

            var conversation = await BotConversations.GetOrCreateAsync(uow, human, bot.User, ct);
            var membership = conversation?.Users.FirstOrDefault(u => u.UserId == human.Id);
            if (conversation is null || membership is null)
                return false;

            var session = await uow.BotSessions.FirstOrDefaultAsync(s => s.BotId == bot.Id && s.UserId == human.Id, ct)
                          ?? BotSession.Create(bot.Id, human.Id);
            var turn = new BotTurn(bot, bot.User, human, conversation, membership, session,
                new BotMenu(Array.Empty<BotMenuNode>()), ct);
            await messenger.SendAsync(turn, text);
            return true;
        }
    }

    // ------------------------------------------------------------------ فهرست

    /// <summary>Status: all | new | seen | doing | done | rejected. Kind: all | idea | bug | opinion.</summary>
    public sealed record GetFeedbackQuery(int Page, int PageSize, string? Status, string? Kind, string? Search) : IQuery<FeedbackPage>;

    internal sealed class GetFeedbackQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetFeedbackQuery, FeedbackPage>
    {
        public async Task<OperationResult<FeedbackPage>> Handle(GetFeedbackQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<FeedbackPage>(AdminPanelKit.Denied());

            var size = Math.Clamp(request.PageSize, 5, 100);
            var page = Math.Max(1, request.Page);

            var countRows = await uow.AppFeedbacks.AsNoTracking()
                .GroupBy(f => f.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);
            int C(string s) => countRows.FirstOrDefault(r => r.Status == s)?.Count ?? 0;
            var counts = new FeedbackCounts(countRows.Sum(r => r.Count),
                C(AppFeedback.StatusNew), C(AppFeedback.StatusSeen), C(AppFeedback.StatusDoing), C(AppFeedback.StatusDone), C(AppFeedback.StatusRejected));

            var query = uow.AppFeedbacks.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
                query = query.Where(f => f.Status == request.Status);
            if (!string.IsNullOrWhiteSpace(request.Kind) && request.Kind != "all")
                query = query.Where(f => f.Kind == request.Kind);

            // متن رمزنگاری شده، پس جستجو در حافظه: شماره، نام فرستنده یا متن
            var search = AdminPanelKit.LatinDigits((request.Search ?? string.Empty).Trim().TrimStart('#'));
            List<FeedbackRow> rows;
            int total;
            if (search.Length == 0)
            {
                total = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((page - 1) * size).Take(size)
                    .Select(f => new { f.Id, f.Number, f.Kind, f.Status, f.Text, f.UserId, f.CreatedAt, HasReply = f.Reply != null })
                    .ToListAsync(ct);
                var names = await NamesAsync(items.Select(i => i.UserId), ct);
                rows = items.Select(i => new FeedbackRow(i.Id, i.Number, i.Kind, i.Status, FeedbackText.Preview(i.Text, 140),
                    i.UserId, names.GetValueOrDefault(i.UserId, "کاربر"), i.CreatedAt, i.HasReply)).ToList();
            }
            else
            {
                var items = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(FeedbackKit.MaxScan)
                    .Select(f => new { f.Id, f.Number, f.Kind, f.Status, f.Text, f.UserId, f.CreatedAt, HasReply = f.Reply != null })
                    .ToListAsync(ct);
                var names = await NamesAsync(items.Select(i => i.UserId), ct);
                var matched = items.Where(i =>
                        i.Number.ToString() == search
                        || names.GetValueOrDefault(i.UserId, "").Contains(search, StringComparison.OrdinalIgnoreCase)
                        || i.Text.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                total = matched.Count;
                rows = matched.Skip((page - 1) * size).Take(size)
                    .Select(i => new FeedbackRow(i.Id, i.Number, i.Kind, i.Status, FeedbackText.Preview(i.Text, 140),
                        i.UserId, names.GetValueOrDefault(i.UserId, "کاربر"), i.CreatedAt, i.HasReply)).ToList();
            }

            return OperationResult.Success(new FeedbackPage(rows, total, page, size, counts));
        }

        private async Task<Dictionary<Guid, string>> NamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            return await uow.Users.AsNoTracking()
                .Where(u => list.Contains(u.Id))
                .Select(u => new { u.Id, u.FirsName, u.LastName })
                .ToDictionaryAsync(u => u.Id, u => AdminPanelKit.Name(u.LastName, u.FirsName), ct);
        }
    }

    // ------------------------------------------------------------------ جزئیات

    public sealed record GetFeedbackItemQuery(Guid Id) : IQuery<FeedbackDetail>;

    internal sealed class GetFeedbackItemQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetFeedbackItemQuery, FeedbackDetail>
    {
        public async Task<OperationResult<FeedbackDetail>> Handle(GetFeedbackItemQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<FeedbackDetail>(AdminPanelKit.Denied());

            var f = await uow.AppFeedbacks.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (f is null)
                return OperationResult.Failure<FeedbackDetail>(new OperationResult().NotFound("بازخورد پیدا نشد."));

            // باز کردن یک بازخورد تازه یعنی «دیده شد»
            if (f.Status == AppFeedback.StatusNew)
            {
                f.SetStatus(AppFeedback.StatusSeen);
                await uow.SaveChangesAsync(ct);
            }

            var user = await uow.Users.AsNoTracking()
                .Where(u => u.Id == f.UserId)
                .Select(u => new { u.FirsName, u.LastName, City = u.Address!.City, u.PhoneNumber })
                .FirstOrDefaultAsync(ct);
            var count = await uow.AppFeedbacks.CountAsync(x => x.UserId == f.UserId, ct);
            var canReply = f.BotId is Guid botId && await uow.Bots.AnyAsync(b => b.Id == botId && !b.Deleted, ct);

            return OperationResult.Success(new FeedbackDetail(
                f.Id, f.Number, f.Kind, f.Status, f.Text,
                f.UserId, user is null ? "کاربر حذف‌شده" : AdminPanelKit.Name(user.LastName, user.FirsName),
                user?.City, AdminPanelKit.MaskPhone(user?.PhoneNumber),
                f.CreatedAt, f.UpdatedAt, f.Reply, f.RepliedAt, f.RepliedByName,
                canReply, count));
        }
    }

    // ------------------------------------------------------------------ وضعیت

    /// <param name="Notify">برای «انجام شد»: به کاربر از ربات خبر داده شود.</param>
    public sealed record SetFeedbackStatusCommand(Guid Id, string Status, bool Notify) : ICommands;

    internal sealed class SetFeedbackStatusCommandHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, BotMessenger messenger)
        : ICommandHandler<SetFeedbackStatusCommand>
    {
        public async Task<OperationResult> Handle(SetFeedbackStatusCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();
            if (!AppFeedback.Statuses.Contains(request.Status))
                return new OperationResult().BadRequest("وضعیت نامعتبر است.");

            var f = await uow.AppFeedbacks.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (f is null)
                return new OperationResult().NotFound("بازخورد پیدا نشد.");

            f.SetStatus(request.Status);
            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            AdminPanelKit.Audit(uow, actor, "feedback.status", "feedback", f.Id, FeedbackText.Number(f.Number), FeedbackText.Status(f.Status));

            var notified = false;
            if (request.Notify && f.Status == AppFeedback.StatusDone)
            {
                var what = f.Kind == AppFeedback.KindBug ? "مشکلی که گزارش داده بودید" : "پیشنهاد شما";
                notified = await FeedbackKit.SendAsync(uow, messenger, f,
                    $"🎉 {what} ({FeedbackText.Number(f.Number)}) انجام شد.\n«{FeedbackText.Preview(f.Text, 120)}»\n\nممنون که کمک کردید ناراگرام بهتر شود.", ct);
            }

            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return new OperationResult().Failed("ذخیره نشد: " + saved.Message);

            return new OperationResult().succedded(notified ? "ذخیره شد و به کاربر خبر داده شد." : "ذخیره شد.");
        }
    }

    // ------------------------------------------------------------------ جواب

    public sealed record ReplyFeedbackCommand(Guid Id, string Text, string? Status) : ICommands;

    internal sealed class ReplyFeedbackCommandHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, BotMessenger messenger)
        : ICommandHandler<ReplyFeedbackCommand>
    {
        public async Task<OperationResult> Handle(ReplyFeedbackCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var text = (request.Text ?? string.Empty).Trim();
            if (text.Length is < 2 or > AppFeedback.MaxReplyLength)
                return new OperationResult().BadRequest("متن جواب باید ۲ تا ۲۰۰۰ حرف باشد.");

            var f = await uow.AppFeedbacks.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (f is null)
                return new OperationResult().NotFound("بازخورد پیدا نشد.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            f.SetReply(text, actor.Name);
            if (!string.IsNullOrWhiteSpace(request.Status) && AppFeedback.Statuses.Contains(request.Status))
                f.SetStatus(request.Status);

            var message =
                $"📬 جواب تیم ناراگرام به {(f.Kind == AppFeedback.KindBug ? "گزارش" : "پیام")} شما ({FeedbackText.Number(f.Number)}):\n\n" +
                text +
                $"\n\n— «{FeedbackText.Preview(f.Text, 100)}»";
            var sent = await FeedbackKit.SendAsync(uow, messenger, f, message, ct);
            if (!sent)
                return new OperationResult().BadRequest("ربات پشتیبانی یا گفتگوی این کاربر پیدا نشد؛ جواب فرستاده نشد.");

            AdminPanelKit.Audit(uow, actor, "feedback.reply", "feedback", f.Id, FeedbackText.Number(f.Number), FeedbackText.Preview(text, 200));
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return new OperationResult().Failed("ذخیره نشد: " + saved.Message);

            return new OperationResult().succedded("جواب برای کاربر فرستاده شد.");
        }
    }
}
