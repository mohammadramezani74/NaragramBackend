using System.Text;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Assistant;
using CleanArchitecture.Application.Bots.Commands;
using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Hr
{
    public interface IKartableDigestService
    {
        /// <summary>ساعت خلاصه‌ی صبح به وقت ایران (Kartable:DigestHour، پیش‌فرض ۸).</summary>
        int Hour { get; }

        /// <summary>ساعت یادآوری عصر برای موارد دیرکرده (Kartable:LateHour، پیش‌فرض ۱۵).</summary>
        int LateHour { get; }

        /// <summary>ساعت کارنامه‌ی هفتگی، چهارشنبه‌ها (Kartable:WeeklyHour، پیش‌فرض ۱۶).</summary>
        int WeeklyHour { get; }

        Task<bool> IsSubscribedAsync(Guid botId, Guid userId, CancellationToken ct);

        /// <summary>هر سه اشتراک: خلاصه‌ی صبح، یادآوری عصر و کارنامه‌ی چهارشنبه.</summary>
        Task SubscribeAsync(Guid botId, Guid userId, CancellationToken ct);
        Task UnsubscribeAsync(Guid botId, Guid userId, CancellationToken ct);

        /// <summary>متن خلاصه‌ی صبح؛ Value خالی (null) یعنی کارتابل خالی است و چیزی نباید فرستاده شود.</summary>
        Task<HrResult<string?>> BuildAsync(string? firstName, string? phone, CancellationToken ct);

        /// <summary>یک دسته از پیام‌های امروزِ فرستاده‌نشده (صبح یا عصر، هر کدام که وقتش است)؛ true اگر کاری بود.</summary>
        Task<bool> RunDueAsync(CancellationToken ct);
    }

    /// <summary>
    /// اعلان‌های کارتابل، روی همان جدول اشتراک ربات دستیار (BotSubscription)؛ تغییری در دیتابیس لازم ندارد:
    ///   «kartable»       خلاصه‌ی صبح: از ساعت Hour تا سه ساعت بعد، اگر چیزی منتظر است.
    ///   «kartable.late»  یادآوری عصر: از ساعت LateHour تا دو ساعت بعد، فقط اگر موردی بیش از
    ///                    LateDays روز مانده باشد.
    ///   «kartable.weekly» کارنامه‌ی هفته: چهارشنبه از ساعت WeeklyHour تا دو ساعت بعد.
    /// هر سه با هم روشن و خاموش می‌شوند. کارتابل خالی، جمعه و تعطیل رسمی چیزی نمی‌فرستد. هر پیام
    /// دکمه‌ی «بریم سراغ کارتابل» دارد (اگر ادمین گزینه‌ی کارتابل را در منوی همان ربات گذاشته باشد).
    /// </summary>
    internal sealed class KartableDigestService(
        IApplicationUnitOfWork uow,
        IApplicationRoleManager roles,
        BotMessenger messenger,
        IIntsConnector ints,
        PersianCalendarService calendar,
        IConfiguration configuration,
        ILogger<KartableDigestService> logger) : IKartableDigestService
    {
        public const string Topic = "kartable";
        public const string LateTopic = "kartable.late";
        public const string WeeklyTopic = "kartable.weekly";
        private static readonly string[] Topics = { Topic, LateTopic, WeeklyTopic };
        public const string FlowKey = "kartable";
        private const int BatchSize = 10;   // هر نفر یک درخواست به INTS است؛ دسته‌ها کوچک بمانند
        private const int LateListed = 5;

        public int Hour => int.TryParse(configuration["Kartable:DigestHour"], out var h) && h is >= 6 and <= 12 ? h : 8;

        public int LateHour => int.TryParse(configuration["Kartable:LateHour"], out var h) && h is >= 12 and <= 18 ? h : 15;

        public int WeeklyHour => int.TryParse(configuration["Kartable:WeeklyHour"], out var h) && h is >= 10 and <= 19 ? h : 16;

        public async Task<bool> IsSubscribedAsync(Guid botId, Guid userId, CancellationToken ct)
            => await uow.BotSubscriptions.AsNoTracking()
                .AnyAsync(s => s.BotId == botId && s.UserId == userId && s.Topic == Topic, ct);

        public async Task SubscribeAsync(Guid botId, Guid userId, CancellationToken ct)
        {
            var existing = await uow.BotSubscriptions.AsNoTracking()
                .Where(s => s.BotId == botId && s.UserId == userId && Topics.Contains(s.Topic))
                .Select(s => s.Topic)
                .ToListAsync(ct);
            foreach (var topic in Topics.Except(existing))
                uow.BotSubscriptions.Add(BotSubscription.Create(botId, userId, topic, city: null));
            await uow.SaveOrThrowAsync(ct);
        }

        public async Task UnsubscribeAsync(Guid botId, Guid userId, CancellationToken ct)
            => await uow.BotSubscriptions
                .Where(s => s.BotId == botId && s.UserId == userId && Topics.Contains(s.Topic))
                .ExecuteDeleteAsync(ct);

        public async Task<HrResult<string?>> BuildAsync(string? firstName, string? phone, CancellationToken ct)
        {
            var r = await KartableApi.GetAsync(ints, phone, ct);
            if (!r.Ok || r.Value is null)
                return HrResult<string?>.Fail(r.Message ?? "کارتابل دریافت نشد.");
            return HrResult<string?>.Success(Morning(firstName, r.Value, IranTime.Now));
        }

        private static string? Morning(string? firstName, List<KartableItem> items, DateTime now)
        {
            if (items.Count == 0) return null;
            var sb = new StringBuilder();
            sb.AppendLine(string.IsNullOrWhiteSpace(firstName) ? "☀️ صبح بخیر!" : $"☀️ صبح بخیر {firstName.Trim()}!");
            sb.AppendLine();
            sb.AppendLine(HrKartableFlow.Summary(items, now));
            sb.AppendLine();
            sb.Append("روز پرباری داشته باشید 🌱");
            return sb.ToString();
        }

        /// <summary>فقط موارد دیرکرده؛ اگر نبود null (پیامی نمی‌رود).</summary>
        private static string? Late(List<KartableItem> items, DateTime now)
        {
            var late = items
                .Where(i => HrKartableFlow.Days(i.SendTime, now) >= HrKartableFlow.LateDays)
                .OrderBy(i => i.SendTime)
                .ToList();
            if (late.Count == 0) return null;

            var sb = new StringBuilder();
            sb.AppendLine(late.Count == 1
                ? $"⏰ یک مورد بیش از {BotText.Number(HrKartableFlow.LateDays)} روز است در کارتابل شما مانده:"
                : $"⏰ {BotText.Number(late.Count)} مورد بیش از {BotText.Number(HrKartableFlow.LateDays)} روز است در کارتابل شما مانده:");
            foreach (var i in late.Take(LateListed))
            {
                var info = KartableTypes.Of(i.Type);
                var from = i.Sender.Length > 0 ? $" از {i.Sender}" : "";
                sb.AppendLine($"{info.Icon} {HrKartableFlow.Name(i)}{from}، {HrKartableFlow.Age(i.SendTime, now)}");
            }
            if (late.Count > LateListed)
                sb.AppendLine($"و {BotText.Number(late.Count - LateListed)} مورد دیگر.");
            sb.AppendLine();
            sb.Append("قبل از پایان روز نگاهی بیندازید؟");
            return sb.ToString();
        }

        public async Task<bool> RunDueAsync(CancellationToken ct)
        {
            var now = IranTime.Now;
            var today = DateOnly.FromDateTime(now);
            if (now.DayOfWeek == DayOfWeek.Friday || calendar.Get(today).Holiday)
                return false;

            var worked = false;
            if (now.Hour >= Hour && now.Hour < Hour + 3)
                worked = await RunTopicAsync(Topic, today, async (human, c) =>
                {
                    var r = await KartableApi.GetAsync(ints, human.PhoneNumber, c);
                    return r.Ok && r.Value is not null ? HrResult<string?>.Success(Morning(human.FirsName, r.Value, now)) : HrResult<string?>.Fail(r.Message ?? "");
                }, ct);
            if (!worked && now.Hour >= LateHour && now.Hour < LateHour + 2)
                worked = await RunTopicAsync(LateTopic, today, async (human, c) =>
                {
                    var r = await KartableApi.GetAsync(ints, human.PhoneNumber, c);
                    return r.Ok && r.Value is not null ? HrResult<string?>.Success(Late(r.Value, now)) : HrResult<string?>.Fail(r.Message ?? "");
                }, ct);
            if (!worked && now.DayOfWeek == DayOfWeek.Wednesday && now.Hour >= WeeklyHour && now.Hour < WeeklyHour + 2)
                worked = await RunTopicAsync(WeeklyTopic, today, async (human, c) =>
                {
                    var w = await KartableApi.GetWeeklyAsync(ints, human.PhoneNumber, c);
                    if (!w.Ok || w.Value is null) return HrResult<string?>.Fail(w.Message ?? "");
                    var pending = await KartableApi.GetAsync(ints, human.PhoneNumber, c);
                    return HrResult<string?>.Success(KartableReport.Build(human.FirsName, w.Value, pending.Value ?? new List<KartableItem>(), now));
                }, ct);
            return worked;
        }

        /// <param name="build">متن پیام این نفر؛ Value خالی یعنی چیزی برای گفتن نیست و پیامی نمی‌رود.</param>
        private async Task<bool> RunTopicAsync(
            string topic, DateOnly today,
            Func<CleanArchitecture.Domain.Entities.Identity.User, CancellationToken, Task<HrResult<string?>>> build,
            CancellationToken ct)
        {
            var batch = await uow.BotSubscriptions
                .Where(s => s.Topic == topic && (s.LastSentOn == null || s.LastSentOn < today))
                .OrderBy(s => s.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);
            if (batch.Count == 0)
                return false;

            var botIds = batch.Select(s => s.BotId).Distinct().ToList();
            var userIds = batch.Select(s => s.UserId).Distinct().ToList();
            var bots = await uow.Bots.Include(b => b.User).Where(b => botIds.Contains(b.Id)).ToListAsync(ct);
            var users = await uow.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(ct);

            // دکمه‌ی «بریم سراغ کارتابل» به گزینه‌ی kartable همان ربات (اگر ادمین در منو گذاشته باشد)
            var nodes = await uow.BotMenuNodes.AsNoTracking()
                .Where(n => botIds.Contains(n.BotId) && n.FlowKey == FlowKey)
                .Select(n => new { n.BotId, n.Id, n.ParamsJson })
                .ToListAsync(ct);

            foreach (var sub in batch)
            {
                // اول علامت می‌خورد تا اگر ارسال این یکی خطا داد، در همین روز دوباره امتحان نشود.
                sub.MarkSent(today);

                var bot = bots.FirstOrDefault(b => b.Id == sub.BotId);
                var human = users.FirstOrDefault(u => u.Id == sub.UserId);
                if (bot?.User is null || !bot.IsEnabled || human is null || human.IsDisabled)
                    continue;
                if (!await BotVisibility.CanUseAsync(bot, human.Id, isAdmin: false, roles, ct))
                    continue;

                try
                {
                    var built = await build(human, ct);
                    if (!built.Ok)
                    {
                        logger.LogInformation("Kartable {Topic} skipped for user {User}: {Message}", topic, sub.UserId, built.Message);
                        continue;
                    }
                    var text = built.Value;
                    if (text is null)
                        continue;   // چیزی برای گفتن نیست: پیامی نمی‌رود

                    var conversation = await BotConversations.GetOrCreateAsync(uow, human, bot.User, ct);
                    var membership = conversation?.Users.FirstOrDefault(u => u.UserId == human.Id);
                    if (conversation is null || membership is null)
                        continue;

                    var session = await uow.BotSessions.FirstOrDefaultAsync(s => s.BotId == bot.Id && s.UserId == human.Id, ct)
                                  ?? BotSession.Create(bot.Id, human.Id);
                    var turn = new BotTurn(bot, bot.User, human, conversation, membership, session,
                        new BotMenu(Array.Empty<BotMenuNode>()), ct);

                    // گزینه‌ی «همه‌ی نوع‌ها» اولویت دارد؛ اگر نبود، هر گزینه‌ی کارتابل
                    var node = nodes.Where(n => n.BotId == bot.Id)
                        .OrderBy(n => string.IsNullOrEmpty(n.ParamsJson) || n.ParamsJson.Contains(HrKartableFlow.AllTypes) ? 0 : 1)
                        .FirstOrDefault();
                    BotReplyMarkup? markup = null;
                    if (node is not null)
                    {
                        markup = new BotReplyMarkup();
                        markup.Rows.Add(new List<BotButton> { new("📂 بریم سراغ کارتابل", BotMenu.NodeData(node.Id), "primary") });
                    }

                    await messenger.SendAsync(turn, text, markup);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Kartable {Topic} failed for user {User}", topic, sub.UserId);
                    await uow.BotSubscriptions.Where(s => s.Id == sub.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSentOn, (DateOnly?)today), CancellationToken.None);
                    return true;
                }
            }

            await uow.SaveChangesAsync(ct);
            return true;
        }
    }

    [BotFlow("kartable.digest", "خلاصه‌ی صبحگاهی کارتابل",
        Description = "اشتراک یا لغو اعلان کارتابل INTS: خلاصه‌ی صبح روزهای کاری، یادآوری عصر برای موارد دیرکرده و کارنامه‌ی چهارشنبه. کارتابل خالی یا روز تعطیل چیزی نمی‌فرستد.")]
    public sealed class HrKartableDigestFlow(IKartableDigestService digest) : BotFlow
    {
        public override async Task RunAsync(BotFlowContext c)
        {
            var subscribed = await c.CallAsync("sub", ct => digest.IsSubscribedAsync(c.BotId, c.User.Id, ct));
            var hour = BotText.ToPersianDigits(digest.Hour.ToString());
            var lateHour = BotText.ToPersianDigits((digest.LateHour > 12 ? digest.LateHour - 12 : digest.LateHour).ToString());
            var weeklyHour = BotText.ToPersianDigits((digest.WeeklyHour > 12 ? digest.WeeklyHour - 12 : digest.WeeklyHour).ToString());

            if (subscribed)
            {
                var action = await c.ChooseAsync(
                    $"🔔 اعلان کارتابل روشن است: هر روز کاری ساعت {hour} صبح خلاصه، ساعت {lateHour} عصر یادآوری اگر موردی بیش از {BotText.Number(HrKartableFlow.LateDays)} روز مانده باشد، و چهارشنبه‌ها ساعت {weeklyHour} کارنامه‌ی هفته.",
                    new[]
                    {
                        new BotOption("off", "🔕 خاموش کن"),
                        new BotOption("keep", "باشد، بماند", "primary"),
                    });
                if (action == "off")
                {
                    await c.CallAsync("off", async ct => { await digest.UnsubscribeAsync(c.BotId, c.User.Id, ct); return true; });
                    await c.SayAsync("🔕 خاموش شد. هر وقت خواستید از همین‌جا روشنش کنید.", menuButton: true);
                }
                else
                {
                    await c.SayAsync("👌", menuButton: true);
                }
                return;
            }

            var preview = await c.CallAsync("preview", ct => digest.BuildAsync(c.User.FirstName, c.User.PhoneNumber, ct));
            if (!preview.Ok)
            {
                // کسی که در INTS کارتابل ندارد (مثلاً شماره‌اش ثبت نیست) مشترک نمی‌شود
                await c.SayAsync("⚠️ " + preview.Message, menuButton: true);
                return;
            }

            var intro = preview.Value is null
                ? "الان کارتابل شما خالی است ✨ اگر روشنش کنید، روزهایی که چیزی منتظرتان باشد صبح خبرتان می‌کنم."
                : "نمونه‌ی امروز:\n\n" + preview.Value;
            await c.SayAsync(intro);

            var ok = await c.ConfirmAsync(
                $"هر روز کاری ساعت {hour} صبح خلاصه‌ی کارتابل را بفرستم؟\n"
                + $"ساعت {lateHour} عصر هم، فقط اگر موردی بیش از {BotText.Number(HrKartableFlow.LateDays)} روز مانده باشد، یک یادآوری کوتاه.\n"
                + $"چهارشنبه‌ها ساعت {weeklyHour} هم کارنامه‌ی هفته: چند مورد بررسی کردید و چقدر سریع.\n"
                + "روزهایی که کارتابل خالی است، جمعه‌ها و تعطیلات رسمی چیزی نمی‌فرستم.",
                yes: "🔔 بله، روشن کن", no: "نه، ممنون");
            if (!ok)
            {
                await c.SayAsync("باشد. هر وقت خواستید از همین‌جا روشنش کنید.", menuButton: true);
                return;
            }

            await c.CallAsync("on", async ct => { await digest.SubscribeAsync(c.BotId, c.User.Id, ct); return true; });
            await c.SayAsync($"✅ روشن شد؛ از فردا ساعت {hour} صبح.", menuButton: true);
        }
    }
}
