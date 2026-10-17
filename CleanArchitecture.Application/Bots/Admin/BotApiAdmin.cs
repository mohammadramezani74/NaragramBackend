using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Application.Bots.Api;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Bots.Admin
{
    // ================================================================== DTOها

    public sealed record BotApiKeyItem(Guid Id, string Name, string Prefix, DateTime CreatedAt, DateTime? LastUsedAt, DateTime? RevokedAt);

    public sealed record BotApiSendItem(
        DateTime At, string KeyName, string Phone, string Status, string? Error, string? ErrorText, string? Reference, string? Preview);

    public sealed record BotApiOverview(
        bool IsOfficial, List<BotApiKeyItem> Keys, List<BotApiSendItem> Recent, int SentToday, int FailedToday);

    /// <summary>Key فقط همین یک بار برگردانده می‌شود.</summary>
    public sealed record CreatedBotApiKey(Guid Id, string Key, string Prefix);

    internal static class BotApiAdminKit
    {
        public const int MaxActiveKeys = 5;

        public static async Task<(Bot? Bot, string Name)> FindAsync(IApplicationUnitOfWork uow, Guid botId, CancellationToken ct)
        {
            var bot = await uow.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == botId, ct);
            return (bot, (bot?.User?.FirsName ?? bot?.Handle ?? string.Empty).Trim());
        }
    }

    // ================================================================== نمای کلی

    public sealed record GetBotApiQuery(Guid BotId) : IQuery<BotApiOverview>;

    internal sealed class GetBotApiQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetBotApiQuery, BotApiOverview>
    {
        public async Task<OperationResult<BotApiOverview>> Handle(GetBotApiQuery request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<BotApiOverview>(denied);

            var bot = await uow.Bots.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.BotId, ct);
            if (bot is null)
                return OperationResult.Failure<BotApiOverview>(new OperationResult().NotFound("ربات یافت نشد."));

            var keys = await uow.BotApiKeys.AsNoTracking()
                .Where(k => k.BotId == bot.Id)
                .OrderBy(k => k.RevokedAt != null).ThenByDescending(k => k.CreateDate)
                .Select(k => new BotApiKeyItem(k.Id, k.Name, k.Prefix, k.CreateDate, k.LastUsedAt, k.RevokedAt))
                .ToListAsync(ct);
            var names = keys.ToDictionary(k => k.Id, k => k.Name);

            var rows = await uow.BotApiSends.AsNoTracking()
                .Where(s => s.BotId == bot.Id)
                .OrderByDescending(s => s.At)
                .Take(30)
                .ToListAsync(ct);

            var today = DateTime.Today;
            var counts = await uow.BotApiSends.AsNoTracking()
                .Where(s => s.BotId == bot.Id && s.At >= today)
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var recent = rows.Select(s => new BotApiSendItem(
                s.At, names.GetValueOrDefault(s.ApiKeyId, "کلید حذف‌شده"), s.Phone, s.Status, s.Error,
                BotApiErrors.Describe(s.Error), s.Reference, s.Preview)).ToList();

            return OperationResult.Success(new BotApiOverview(
                bot.IsOfficial,
                keys,
                recent,
                counts.FirstOrDefault(c => c.Status == BotApiSend.StatusSent)?.Count ?? 0,
                counts.FirstOrDefault(c => c.Status == BotApiSend.StatusFailed)?.Count ?? 0));
        }
    }

    // ================================================================== کلیدها

    public sealed record CreateBotApiKeyCommand(Guid BotId, string Name) : ICommands<CreatedBotApiKey>;

    internal sealed class CreateBotApiKeyCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<CreateBotApiKeyCommand, CreatedBotApiKey>
    {
        public async Task<OperationResult<CreatedBotApiKey>> Handle(CreateBotApiKeyCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<CreatedBotApiKey>(denied);

            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length is < 2 or > BotApiKey.MaxNameLength)
                return Bad($"نام کلید (برای کدام سیستم است) ۲ تا {BotApiKey.MaxNameLength} نویسه است.");

            var (bot, botName) = await BotApiAdminKit.FindAsync(uow, request.BotId, ct);
            if (bot is null)
                return OperationResult.Failure<CreatedBotApiKey>(new OperationResult().NotFound("ربات یافت نشد."));

            var active = await uow.BotApiKeys.CountAsync(k => k.BotId == bot.Id && k.RevokedAt == null, ct);
            if (active >= BotApiAdminKit.MaxActiveKeys)
                return Bad($"این ربات {BotApiAdminKit.MaxActiveKeys} کلید فعال دارد؛ اول یکی را باطل کنید.");

            var actor = await AdminPanelKit.ActorAsync(users, uow, ct);
            var (plain, prefix, hash) = BotApiKeys.Generate();
            var key = BotApiKey.Create(bot.Id, name, prefix, hash, actor.Id);
            uow.BotApiKeys.Add(key);
            AdminPanelKit.Audit(uow, actor, "bot.key.create", "bot", bot.Id, botName, $"{name} · {prefix}…");

            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess
                ? OperationResult.Success(new CreatedBotApiKey(key.Id, plain, prefix))
                : OperationResult.Failure<CreatedBotApiKey>(new OperationResult().Failed("ذخیره نشد: " + saved.Message));
        }

        private static OperationResult<CreatedBotApiKey> Bad(string message)
            => OperationResult.Failure<CreatedBotApiKey>(new OperationResult().BadRequest(message));
    }

    public sealed record RevokeBotApiKeyCommand(Guid BotId, Guid KeyId) : ICommands;

    internal sealed class RevokeBotApiKeyCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<RevokeBotApiKeyCommand>
    {
        public async Task<OperationResult> Handle(RevokeBotApiKeyCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return denied;

            var key = await uow.BotApiKeys.FirstOrDefaultAsync(k => k.Id == request.KeyId && k.BotId == request.BotId, ct);
            if (key is null)
                return new OperationResult().NotFound("کلید یافت نشد.");
            if (!key.IsActive)
                return new OperationResult().succedded("این کلید قبلاً باطل شده بود.");

            var (_, botName) = await BotApiAdminKit.FindAsync(uow, request.BotId, ct);
            key.Revoke();
            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "bot.key.revoke", "bot", request.BotId, botName, $"{key.Name} · {key.Prefix}…");

            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess ? new OperationResult().succedded("کلید باطل شد.") : new OperationResult().Failed("ذخیره نشد: " + saved.Message);
        }
    }

    // ================================================================== ربات رسمی

    public sealed record SetBotOfficialCommand(Guid BotId, bool IsOfficial) : ICommands;

    internal sealed class SetBotOfficialCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, IApplicationUserManager users)
        : ICommandHandler<SetBotOfficialCommand>
    {
        public async Task<OperationResult> Handle(SetBotOfficialCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return denied;

            var (bot, botName) = await BotApiAdminKit.FindAsync(uow, request.BotId, ct);
            if (bot is null)
                return new OperationResult().NotFound("ربات یافت نشد.");
            if (bot.IsOfficial == request.IsOfficial)
                return new OperationResult().succedded();

            bot.SetOfficial(request.IsOfficial);
            AdminPanelKit.Audit(uow, await AdminPanelKit.ActorAsync(users, uow, ct), "bot.official", "bot", bot.Id, botName,
                request.IsOfficial ? "رسمی شد" : "از حالت رسمی بیرون آمد");

            var saved = await uow.SaveChangesAsync(ct);
            return saved.IsSuccess ? new OperationResult().succedded() : new OperationResult().Failed("ذخیره نشد: " + saved.Message);
        }
    }
}
