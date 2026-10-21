using System.Text;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Commands;
using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Assistant
{
    public sealed record DigestSubscriptionInfo(bool Subscribed, string? City);

    public interface IAssistantDigestService
    {
        /// <summary>ساعت ارسال به وقت ایران (Assistant:DigestHour، پیش‌فرض ۸).</summary>
        int Hour { get; }

        Task<DigestSubscriptionInfo> GetAsync(Guid botId, Guid userId, CancellationToken ct);
        Task<bool> SubscribeAsync(Guid botId, Guid userId, string city, CancellationToken ct);
        Task<bool> UnsubscribeAsync(Guid botId, Guid userId, CancellationToken ct);
        Task<string> BuildAsync(string? firstName, string city, CancellationToken ct);

        /// <summary>یک دسته از خلاصه‌های امروزِ فرستاده‌نشده را می‌فرستد؛ true اگر کاری بود.</summary>
        Task<bool> RunDueAsync(CancellationToken ct);
    }

    /// <summary>
    /// «خلاصه‌ی صبحگاهی» ربات دستیار: هر روز از ساعت Hour تا سه ساعت بعد، برای هر مشترک یک پیام
    /// (تاریخ و مناسبت، هوای شهرش، قیمت‌ها، اذان ظهر و مغرب). اگر سرور بعد از این بازه بالا آمد،
    /// آن روز فرستاده نمی‌شود تا خبر کهنه نرسد. LastSentOn جلوی ارسال تکراری را می‌گیرد.
    /// </summary>
    internal sealed class AssistantDigestService(
        IApplicationUnitOfWork uow,
        IApplicationRoleManager roles,
        BotMessenger messenger,
        IWeatherSource weather,
        IPriceSource prices,
        PersianCalendarService calendar,
        IConfiguration configuration,
        ILogger<AssistantDigestService> logger) : IAssistantDigestService
    {
        public const string Topic = "daily";
        private const int BatchSize = 25;

        public int Hour => int.TryParse(configuration["Assistant:DigestHour"], out var h) && h is >= 5 and <= 12 ? h : 8;

        public async Task<DigestSubscriptionInfo> GetAsync(Guid botId, Guid userId, CancellationToken ct)
        {
            var sub = await uow.BotSubscriptions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.BotId == botId && s.UserId == userId && s.Topic == Topic, ct);
            return new DigestSubscriptionInfo(sub is not null, sub?.City);
        }

        public async Task<bool> SubscribeAsync(Guid botId, Guid userId, string city, CancellationToken ct)
        {
            var sub = await uow.BotSubscriptions.FirstOrDefaultAsync(s => s.BotId == botId && s.UserId == userId && s.Topic == Topic, ct);
            if (sub is null)
                uow.BotSubscriptions.Add(BotSubscription.Create(botId, userId, Topic, city));
            else
                sub.SetCity(city);
            await uow.SaveOrThrowAsync(ct);
            return true;
        }

        public async Task<bool> UnsubscribeAsync(Guid botId, Guid userId, CancellationToken ct)
        {
            await uow.BotSubscriptions
                .Where(s => s.BotId == botId && s.UserId == userId && s.Topic == Topic)
                .ExecuteDeleteAsync(ct);
            return true;
        }

        public async Task<string> BuildAsync(string? firstName, string cityName, CancellationToken ct)
        {
            var city = AssistantCities.Find(cityName) ?? AssistantCities.Find("تهران")!;
            var today = IranTime.Today;
            var day = calendar.Get(today);

            var weatherTask = weather.GetAsync(city, ct);
            var pricesTask = prices.GetAsync(ct);
            await Task.WhenAll(weatherTask, pricesTask);
            var w = weatherTask.Result;
            var p = pricesTask.Result;
            var times = PrayerTimes.Compute(city, today);

            var sb = new StringBuilder();
            sb.AppendLine(string.IsNullOrWhiteSpace(firstName) ? "☀️ صبح بخیر!" : $"☀️ صبح بخیر {firstName.Trim()}!");
            sb.AppendLine($"📅 {day.Weekday} {day.Persian}");
            sb.AppendLine($"{day.Hijri} · {day.Gregorian}");
            foreach (var o in day.Occasions)
                sb.AppendLine((o.Holiday ? "🎉 " : "• ") + o.Title + (o.Holiday ? " (تعطیل)" : string.Empty));

            sb.AppendLine();
            if (w.Ok)
            {
                var line = $"{w.Emoji} هوای {city.Name}: الان {AssistantFormat.Degrees(w.Temp)}، {w.Text}";
                if (w.Days.FirstOrDefault() is { } d)
                    line += $"؛ امروز {AssistantFormat.Degrees(d.Min)} تا {AssistantFormat.Degrees(d.Max)}";
                sb.AppendLine(line);
            }

            if (p.Ok)
            {
                var picks = new[] { "usd", "coin_emami", "gold18" }
                    .Select(k => p.Items.FirstOrDefault(i => i.Key == k))
                    .Where(i => i is not null)
                    .Select(i => $"{ShortTitle(i!.Key)} {BotText.Money(i.Toman)}")
                    .ToList();
                if (picks.Count > 0)
                    sb.AppendLine("💵 " + string.Join(" · ", picks));
            }

            sb.AppendLine($"🕌 اذان ظهر {city.Name} {times.Dhuhr} · اذان مغرب {times.Maghrib}");
            sb.AppendLine();
            sb.Append("روز خوبی داشته باشید 🌱");
            return sb.ToString();
        }

        private static string ShortTitle(string key) => key switch
        {
            "usd" => "دلار",
            "coin_emami" => "سکه امامی",
            "gold18" => "طلای ۱۸",
            _ => key,
        };

        public async Task<bool> RunDueAsync(CancellationToken ct)
        {
            var now = IranTime.Now;
            if (now.Hour < Hour || now.Hour >= Hour + 3)
                return false;
            var today = DateOnly.FromDateTime(now);

            var batch = await uow.BotSubscriptions
                .Where(s => s.Topic == Topic && (s.LastSentOn == null || s.LastSentOn < today))
                .OrderBy(s => s.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);
            if (batch.Count == 0)
                return false;

            var botIds = batch.Select(s => s.BotId).Distinct().ToList();
            var userIds = batch.Select(s => s.UserId).Distinct().ToList();
            var bots = await uow.Bots.Include(b => b.User).Where(b => botIds.Contains(b.Id)).ToListAsync(ct);
            var users = await uow.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(ct);

            foreach (var sub in batch)
            {
                // اول علامت می‌خورد تا اگر ارسال این یکی خطا داد، در همین روز دوباره و دوباره امتحان نشود.
                sub.MarkSent(today);

                var bot = bots.FirstOrDefault(b => b.Id == sub.BotId);
                var human = users.FirstOrDefault(u => u.Id == sub.UserId);
                if (bot?.User is null || !bot.IsEnabled || human is null || human.IsDisabled)
                    continue;
                if (!await BotVisibility.CanUseAsync(bot, human.Id, isAdmin: false, roles, ct))
                    continue;

                try
                {
                    var text = await BuildAsync(human.FirsName, sub.City ?? human.Address?.City ?? "تهران", ct);
                    var conversation = await BotConversations.GetOrCreateAsync(uow, human, bot.User, ct);
                    var membership = conversation?.Users.FirstOrDefault(u => u.UserId == human.Id);
                    if (conversation is null || membership is null)
                        continue;

                    var session = await uow.BotSessions.FirstOrDefaultAsync(s => s.BotId == bot.Id && s.UserId == human.Id, ct)
                                  ?? BotSession.Create(bot.Id, human.Id);
                    var turn = new BotTurn(bot, bot.User, human, conversation, membership, session,
                        new BotMenu(Array.Empty<BotMenuNode>()), ct);
                    await messenger.SendAsync(turn, text);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Assistant digest failed for user {User}", sub.UserId);
                    // ذخیره‌ی نیمه‌کاره در Change Tracker مانده؛ این یکی را بیرون از آن علامت بزن و دسته را
                    // تمام کن تا دور بعد با scope تازه ادامه دهد.
                    await uow.BotSubscriptions.Where(s => s.Id == sub.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSentOn, (DateOnly?)today), CancellationToken.None);
                    return true;
                }
            }

            await uow.SaveChangesAsync(ct);
            return true;
        }
    }
}
