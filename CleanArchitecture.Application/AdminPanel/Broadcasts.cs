using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots;
using CleanArchitecture.Application.Bots.Commands;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Admin;
using CleanArchitecture.Domain.Entities.Bots;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.AdminPanel
{
    // ================================================================== DTOها

    /// <summary>Mode: all | roles | cities | users</summary>
    public sealed record BroadcastAudience(string Mode, List<Guid>? RoleIds, List<string>? Cities, List<Guid>? UserIds);

    public sealed record BroadcastAudiencePreview(int Count, List<string> Sample, string Label);

    public sealed record BroadcastRow(
        Guid Id, string Title, string Text, string Kind, string? ButtonText, string? ButtonUrl,
        string AudienceLabel, string Status, DateTime ScheduledAt, DateTime? StartedAt, DateTime? FinishedAt,
        int Total, int Delivered, int Seen, string CreatedByName, string? Error);

    public sealed record BroadcastsPage(List<BroadcastRow> Items, int Total, int Page, int PageSize, string BotName, Guid? BotUserId);

    // ================================================================== کمک‌ها

    internal static class BroadcastKit
    {
        public const string BotHandle = "naragram";
        public const string BotName = "اطلاع‌رسانی ناراگرام";
        public const string BotIntro = "این ربات پیام‌های همگانی سازمان را می‌فرستد. اطلاعیه‌ها همین‌جا می‌رسند.";

        public static string Status(BroadcastStatus s) => s switch
        {
            BroadcastStatus.Scheduled => "scheduled",
            BroadcastStatus.Sending => "sending",
            BroadcastStatus.Sent => "sent",
            BroadcastStatus.Cancelled => "cancelled",
            _ => "failed",
        };

        public static bool IsValidUrl(string? url)
            => Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp);

        /// <summary>گیرنده‌ها: کاربرهای واقعی و فعال (نه ربات، نه غیرفعال‌شده).</summary>
        public static async Task<List<Guid>> ResolveAsync(IApplicationUnitOfWork uow, IAdminIdentityService identity, BroadcastAudience audience, CancellationToken ct)
        {
            var people = uow.Users.AsNoTracking().Where(u => !u.IsBot && !u.IsDisabled);
            switch (audience.Mode)
            {
                case "roles":
                    var ids = new HashSet<Guid>();
                    foreach (var roleId in audience.RoleIds ?? new List<Guid>())
                        ids.UnionWith(await identity.UserIdsInRoleAsync(roleId, ct));
                    var roleIds = ids.ToList();
                    people = people.Where(u => roleIds.Contains(u.Id));
                    break;
                case "cities":
                    var cities = (audience.Cities ?? new List<string>()).Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
                    people = people.Where(u => cities.Contains(u.Address!.City));
                    break;
                case "users":
                    var userIds = audience.UserIds ?? new List<Guid>();
                    people = people.Where(u => userIds.Contains(u.Id));
                    break;
                default:
                    break; // all
            }
            return await people.Select(u => u.Id).ToListAsync(ct);
        }

        public static async Task<string> LabelAsync(IAdminIdentityService identity, BroadcastAudience audience, int count, CancellationToken ct)
        {
            switch (audience.Mode)
            {
                case "roles":
                    var roles = await identity.RolesAsync(ct);
                    var names = roles.Where(r => (audience.RoleIds ?? new()).Contains(r.Id)).Select(r => r.Name).ToList();
                    return "نقش‌ها: " + string.Join("، ", names);
                case "cities":
                    return "شهرها: " + string.Join("، ", audience.Cities ?? new List<string>());
                case "users":
                    return $"{count} نفر انتخاب‌شده";
                default:
                    return "همه‌ی کاربران";
            }
        }

        /// <summary>ربات سیستمی؛ اگر نیست ساخته می‌شود (روشن، برای همه).</summary>
        public static async Task<(Bot Bot, User BotUser)> EnsureBotAsync(IApplicationUnitOfWork uow, IApplicationUserManager users, Guid createdBy, CancellationToken ct)
        {
            var bot = await uow.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.Handle == BotHandle, ct);
            if (bot?.User is not null)
            {
                if (!bot.IsEnabled || bot.AccessMode != BotAccessMode.All)
                {
                    bot.SetEnabled(true);
                    bot.SetAccess(BotAccessMode.All, "[]");
                    await uow.SaveOrThrowAsync(ct);
                }
                return (bot, bot.User);
            }

            var created = await users.CreateBotUserAsync("bot_" + BotHandle, BotName, ct);
            if (!created.IsSucceded)
                throw new InvalidOperationException("ساختن ربات اطلاع‌رسانی ممکن نشد: " + created.Message);

            bot = Bot.Create(created.result, BotHandle, BotIntro, createdBy);
            bot.SetEnabled(true);
            bot.SetAccess(BotAccessMode.All, "[]");
            uow.Bots.Add(bot);
            uow.BotMenuNodes.Add(BotMenuNode.CreateRoot(bot.Id, BotIntro));
            await uow.SaveOrThrowAsync(ct);

            var botUser = await uow.Users.FirstAsync(u => u.Id == created.result, ct);
            return (bot, botUser);
        }

        public static BotReplyMarkup Markup(Broadcast b)
        {
            var markup = new BotReplyMarkup
            {
                Note = b.Kind switch
                {
                    "urgent" => "اطلاعیه‌ی فوری",
                    "reminder" => "یادآوری",
                    _ => "اطلاعیه‌ی سازمان",
                },
                NoteTone = b.Kind == "urgent" ? "warn" : "muted",
            };
            if (!string.IsNullOrWhiteSpace(b.ButtonText) && IsValidUrl(b.ButtonUrl))
                markup.Rows.Add(new List<BotButton> { new(b.ButtonText!, "link", null, b.ButtonUrl) });
            return markup;
        }
    }

    // ================================================================== پیش‌نمایش گیرنده‌ها

    public sealed record PreviewBroadcastAudienceQuery(BroadcastAudience Audience) : IQuery<BroadcastAudiencePreview>;

    internal sealed class PreviewBroadcastAudienceQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IAdminIdentityService identity)
        : IQueryHandler<PreviewBroadcastAudienceQuery, BroadcastAudiencePreview>
    {
        public async Task<OperationResult<BroadcastAudiencePreview>> Handle(PreviewBroadcastAudienceQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<BroadcastAudiencePreview>(AdminPanelKit.Denied());

            var ids = await BroadcastKit.ResolveAsync(uow, identity, request.Audience, ct);
            var sampleIds = ids.Take(6).ToList();
            var sample = await uow.Users.AsNoTracking()
                .Where(u => sampleIds.Contains(u.Id))
                .Select(u => new { u.LastName, u.FirsName })
                .ToListAsync(ct);

            return OperationResult.Success(new BroadcastAudiencePreview(
                ids.Count,
                sample.Select(s => AdminPanelKit.Name(s.LastName, s.FirsName)).ToList(),
                await BroadcastKit.LabelAsync(identity, request.Audience, ids.Count, ct)));
        }
    }

    // ================================================================== ساختن و لغو

    public sealed record CreateBroadcastCommand(
        string Title, string Text, string Kind, string? ButtonText, string? ButtonUrl,
        BroadcastAudience Audience, DateTime? ScheduledAt) : ICommands<Guid>;

    internal sealed class CreateBroadcastCommandHandler(
        IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users, IAdminIdentityService identity)
        : ICommandHandler<CreateBroadcastCommand, Guid>
    {
        public async Task<OperationResult<Guid>> Handle(CreateBroadcastCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<Guid>(AdminPanelKit.Denied());

            var text = (request.Text ?? string.Empty).Trim();
            var title = (request.Title ?? string.Empty).Trim();
            if (title.Length == 0)
                title = text.Split('\n')[0].Trim();
            if (title.Length > Broadcast.MaxTitleLength)
                title = title[..Broadcast.MaxTitleLength];

            if (text.Length is < 2 or > Broadcast.MaxTextLength)
                return Bad($"متن پیام باید ۲ تا {Broadcast.MaxTextLength} حرف باشد.");
            if (request.Kind is not ("info" or "urgent" or "reminder"))
                return Bad("نوع پیام نامعتبر است.");

            var buttonText = string.IsNullOrWhiteSpace(request.ButtonText) ? null : request.ButtonText.Trim();
            var buttonUrl = string.IsNullOrWhiteSpace(request.ButtonUrl) ? null : request.ButtonUrl.Trim();
            if ((buttonText is null) != (buttonUrl is null))
                return Bad("برای دکمه، هم متن و هم لینک لازم است.");
            if (buttonText is { Length: > Broadcast.MaxButtonTextLength })
                return Bad("متن دکمه خیلی بلند است.");
            if (buttonUrl is not null && (!BroadcastKit.IsValidUrl(buttonUrl) || buttonUrl.Length > Broadcast.MaxUrlLength))
                return Bad("لینک دکمه باید با http:// یا https:// شروع شود.");

            var audience = request.Audience ?? new BroadcastAudience("all", null, null, null);
            if (audience.Mode is not ("all" or "roles" or "cities" or "users"))
                return Bad("گیرنده‌ها نامعتبر است.");

            var recipients = await BroadcastKit.ResolveAsync(uow, identity, audience, ct);
            if (recipients.Count == 0)
                return Bad("با این انتخاب، پیام به کسی نمی‌رسد.");

            var now = DateTime.Now;
            var at = request.ScheduledAt is DateTime s && s > now ? s : now;
            if (at > now.AddDays(60))
                return Bad("زمان ارسال حداکثر ۶۰ روز بعد است.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            var label = await BroadcastKit.LabelAsync(identity, audience, recipients.Count, ct);
            var broadcast = Broadcast.Create(title, text, request.Kind, buttonText, buttonUrl,
                BotJson.Serialize(audience), label, at, actor.Id, actor.Name);
            uow.Broadcasts.Add(broadcast);
            AdminPanelKit.Audit(uow, actor, "broadcast.create", "broadcast", broadcast.Id, title,
                $"{label} · {recipients.Count} نفر" + (at > now.AddMinutes(1) ? " · زمان‌بندی‌شده" : string.Empty));

            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess
                ? OperationResult.Success(broadcast.Id)
                : OperationResult.Failure<Guid>(new OperationResult().Failed("ذخیره نشد: " + saved.Message));
        }

        private static OperationResult<Guid> Bad(string message)
            => OperationResult.Failure<Guid>(new OperationResult().BadRequest(message));
    }

    public sealed record CancelBroadcastCommand(Guid Id) : ICommands;

    internal sealed class CancelBroadcastCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<CancelBroadcastCommand>
    {
        public async Task<OperationResult> Handle(CancelBroadcastCommand request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return AdminPanelKit.Denied();

            var b = await uow.Broadcasts.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (b is null)
                return new OperationResult().NotFound("پیام یافت نشد.");
            if (!b.Cancel())
                return new OperationResult().BadRequest("فقط پیامی که هنوز ارسال نشده لغو می‌شود.");

            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "broadcast.cancel", "broadcast", b.Id, b.Title);
            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess ? new OperationResult().succedded() : new OperationResult().Failed("ذخیره نشد: " + saved.Message);
        }
    }

    // ================================================================== تاریخچه

    public sealed record GetBroadcastsQuery(int Page, int PageSize) : IQuery<BroadcastsPage>;

    internal sealed class GetBroadcastsQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetBroadcastsQuery, BroadcastsPage>
    {
        public async Task<OperationResult<BroadcastsPage>> Handle(GetBroadcastsQuery request, CancellationToken ct)
        {
            if (!admin.CurrentUserIsAdmin)
                return OperationResult.Failure<BroadcastsPage>(AdminPanelKit.Denied());

            var page = Math.Max(1, request.Page);
            var size = Math.Clamp(request.PageSize, 5, 50);
            var total = await uow.Broadcasts.CountAsync(ct);
            var rows = await uow.Broadcasts.AsNoTracking()
                .OrderByDescending(b => b.ScheduledAt)
                .Skip((page - 1) * size).Take(size)
                .ToListAsync(ct);

            var ids = rows.Select(r => r.Id).ToList();
            var seen = await (
                    from d in uow.BroadcastDeliveries.AsNoTracking()
                    join m in uow.Messages.AsNoTracking() on d.MessageId equals m.Id
                    where ids.Contains(d.BroadcastId) && m.Seen
                    group d by d.BroadcastId into g
                    select new { Id = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var bot = await uow.Bots.AsNoTracking().Where(b => b.Handle == BroadcastKit.BotHandle)
                .Select(b => new { b.UserId }).FirstOrDefaultAsync(ct);

            var items = rows.Select(b => new BroadcastRow(
                b.Id, b.Title, b.Text, b.Kind, b.ButtonText, b.ButtonUrl, b.AudienceLabel,
                BroadcastKit.Status(b.Status), b.ScheduledAt, b.StartedAt, b.FinishedAt,
                b.Total, b.Delivered, seen.FirstOrDefault(s => s.Id == b.Id)?.Count ?? 0,
                b.CreatedByName, b.Error)).ToList();

            return OperationResult.Success(new BroadcastsPage(items, total, page, size, BroadcastKit.BotName, bot?.UserId));
        }
    }

    // ================================================================== ارسال در پس‌زمینه

    public interface IBroadcastSender
    {
        /// <summary>یک پیام همگانیِ موعدرسیده (یا نیمه‌کاره) را می‌فرستد؛ true اگر کاری بود.</summary>
        Task<bool> RunDueAsync(CancellationToken ct);
    }

    internal sealed class BroadcastSender(
        IApplicationUnitOfWork uow,
        IApplicationUserManager users,
        IAdminIdentityService identity,
        IHubContext<NaraHub, IChatHubClient> hub,
        INotificationService notifications,
        ILogger<BroadcastSender> logger) : IBroadcastSender
    {
        private const int BatchSize = 20;

        public async Task<bool> RunDueAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            var due = await uow.Broadcasts
                .Where(b => b.Status == BroadcastStatus.Sending || (b.Status == BroadcastStatus.Scheduled && b.ScheduledAt <= now))
                .OrderBy(b => b.ScheduledAt)
                .FirstOrDefaultAsync(ct);
            if (due is null)
                return false;

            try
            {
                await SendAsync(due, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Broadcast {Id} failed", due.Id);
                due.Fail(ex.Message);
                await uow.SaveChangesAsync(ct);
            }
            return true;
        }

        private async Task SendAsync(Broadcast b, CancellationToken ct)
        {
            var audience = BotJson.Deserialize<BroadcastAudience>(b.AudienceJson) ?? new BroadcastAudience("all", null, null, null);
            var recipients = await BroadcastKit.ResolveAsync(uow, identity, audience, ct);
            var done = await uow.BroadcastDeliveries.AsNoTracking()
                .Where(d => d.BroadcastId == b.Id).Select(d => d.UserId).ToListAsync(ct);
            var todo = recipients.Except(done).ToList();

            b.Start(Math.Max(recipients.Count, done.Count));
            b.Progress(done.Count);
            await uow.SaveOrThrowAsync(ct);

            var (_, botUser) = await BroadcastKit.EnsureBotAsync(uow, users, b.CreatedByUserId ?? Guid.Empty, ct);
            var markupJson = BroadcastKit.Markup(b).ToJson();
            var preview = Preview(b.Text);
            var botName = string.IsNullOrWhiteSpace(botUser.FirsName) ? BroadcastKit.BotName : botUser.FirsName!;
            var delivered = done.Count;

            foreach (var batch in todo.Chunk(BatchSize))
            {
                var pushes = new List<(Guid UserId, ChatMessageDto Dto)>();
                foreach (var userId in batch)
                {
                    var human = await uow.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
                    if (human is null) continue;

                    var conversation = await BotConversations.GetOrCreateAsync(uow, human, botUser, ct);
                    if (conversation is null) continue;
                    var membership = conversation.Users.FirstOrDefault(cu => cu.UserId == userId);

                    var message = conversation.AddMessage(b.Text, botUser);
                    message.SetReplyMarkup(markupJson);
                    conversation.LastMessageText = preview;
                    conversation.LastMessageId = message.Id;
                    conversation.LastUserSenderMessageId = botUser.Id;
                    conversation.LastMessageSentAt = message.CreateDate;
                    membership?.IncreaseCount();
                    uow.BroadcastDeliveries.Add(BroadcastDelivery.Create(b.Id, userId, message.Id));

                    pushes.Add((userId, new ChatMessageDto
                    {
                        Id = message.Id,
                        ScopeId = conversation.Id,
                        Content = message.Content,
                        IsMine = false,
                        SendAt = message.CreateDate,
                        SenderName = botName,
                        Type = (int)message.MessageType,
                        UserId = botUser.Id,
                        IsSeen = false,
                        ReplyMarkup = message.ReplyMarkup,
                        ConversationType = ConversationTyped.Private,
                    }));
                }

                delivered += pushes.Count;
                b.Progress(delivered);
                await uow.SaveOrThrowAsync(ct);

                foreach (var (userId, dto) in pushes)
                {
                    try
                    {
                        var key = userId.ToString();
                        await hub.Clients.User(key).IncreaseMessageCount(botUser.Id);
                        await hub.Clients.User(key).MessagedReceived(dto);
                        await hub.Clients.User(key).ReceivedNotifications(new NotificationModelDto(botName, botUser.Avatar ?? string.Empty, preview, string.Empty));
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Broadcast push failed for {User}", userId);
                    }
                }

                var batchIds = pushes.Select(p => p.UserId).ToList();
                var tokens = await uow.FireBaseTokens.AsNoTracking()
                    .Where(t => batchIds.Contains(t.UserId))
                    .Select(t => t.Token)
                    .ToListAsync(ct);
                if (tokens.Count > 0)
                    _ = notifications.SendToMany(tokens, botName, preview);
            }

            b.Finish();
            await uow.SaveOrThrowAsync(ct);
        }

        private static string Preview(string text)
        {
            var firstLine = (text ?? string.Empty).Split('\n')[0].Trim();
            return firstLine.Length > 40 ? firstLine[..40] + "..." : firstLine;
        }
    }
}
