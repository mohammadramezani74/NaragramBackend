using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Bots;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace CleanArchitecture.Application.Bots.Admin
{
    // ================================================================== DTOها

    public sealed record BotAdminListItem(Guid Id, Guid BotUserId, string Name, string Handle, bool IsEnabled, int StartedCount);

    public sealed record BotMenuNodeDto(
        Guid Id,
        Guid? ParentId,
        int SortOrder,
        string Label,
        string? Text,
        BotMenuNodeKind Kind,
        BotMenuDisplay Display,
        string? FlowKey,
        Dictionary<string, string>? Params);

    public sealed record BotAdminDetail(
        Guid Id,
        Guid BotUserId,
        string Name,
        string Handle,
        string? Description,
        string? Avatar,
        bool IsEnabled,
        BotAccessMode AccessMode,
        List<Guid> AccessList,
        List<BotCommandItem> Commands,
        List<BotMenuNodeDto> Nodes,
        int StartedCount);

    public sealed record BotFlowInfo(string Key, string Title, string? Description, List<BotFlowParamDescriptor> Params);

    public sealed record BotConnectorInfo(string Key, string Title, string? Description);

    internal static class BotAdminGuard
    {
        public static OperationResult? Check(IAdminAccess admin)
            => admin.CurrentUserIsAdmin ? null : new OperationResult().NotFound("یافت نشد.");

        public static readonly string[] ReservedCommands = { "/شروع", "/start", "/انصراف", "/cancel", "/راهنما", "/help" };
    }

    // ================================================================== فهرست

    public sealed record GetBotsAdminQuery : IQuery<List<BotAdminListItem>>;

    internal sealed class GetBotsAdminQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetBotsAdminQuery, List<BotAdminListItem>>
    {
        public async Task<OperationResult<List<BotAdminListItem>>> Handle(GetBotsAdminQuery request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<List<BotAdminListItem>>(denied);

            var started = await uow.BotSessions.AsNoTracking()
                .Where(s => s.StartedAt != null)
                .GroupBy(s => s.BotId)
                .Select(g => new { BotId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BotId, x => x.Count, ct);

            var bots = await uow.Bots.AsNoTracking()
                .OrderBy(b => b.CreateDate)
                .Select(b => new { b.Id, b.UserId, Name = b.User!.FirsName, b.Handle, b.IsEnabled })
                .ToListAsync(ct);

            return OperationResult.Success(bots
                .Select(b => new BotAdminListItem(b.Id, b.UserId, b.Name ?? b.Handle, b.Handle, b.IsEnabled, started.GetValueOrDefault(b.Id)))
                .ToList());
        }
    }

    // ================================================================== جزئیات

    public sealed record GetBotAdminQuery(Guid Id) : IQuery<BotAdminDetail>;

    internal sealed class GetBotAdminQueryHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : IQueryHandler<GetBotAdminQuery, BotAdminDetail>
    {
        public async Task<OperationResult<BotAdminDetail>> Handle(GetBotAdminQuery request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<BotAdminDetail>(denied);

            var detail = await BotAdminLoader.LoadAsync(uow, request.Id, ct);
            return detail is null
                ? OperationResult.Failure<BotAdminDetail>(new OperationResult().NotFound("ربات یافت نشد."))
                : OperationResult.Success(detail);
        }
    }

    internal static class BotAdminLoader
    {
        public static async Task<BotAdminDetail?> LoadAsync(IApplicationUnitOfWork uow, Guid id, CancellationToken ct)
        {
            var bot = await uow.Bots.AsNoTracking().Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bot?.User is null)
                return null;

            var nodes = await uow.BotMenuNodes.AsNoTracking()
                .Where(n => n.BotId == id)
                .OrderBy(n => n.SortOrder)
                .ToListAsync(ct);

            var startedCount = await uow.BotSessions.AsNoTracking()
                .CountAsync(s => s.BotId == id && s.StartedAt != null, ct);

            return new BotAdminDetail(
                bot.Id,
                bot.UserId,
                bot.User.FirsName ?? bot.Handle,
                bot.Handle,
                bot.Description,
                bot.User.Avatar,
                bot.IsEnabled,
                bot.AccessMode,
                BotVisibility.ParseIds(bot.AccessListJson),
                BotJson.Deserialize<List<BotCommandItem>>(bot.CommandsJson) ?? new List<BotCommandItem>(),
                nodes.Select(n => new BotMenuNodeDto(
                    n.Id, n.ParentId, n.SortOrder, n.Label, n.Text, n.Kind, n.Display, n.FlowKey,
                    BotJson.Deserialize<Dictionary<string, string>>(n.ParamsJson))).ToList(),
                startedCount);
        }
    }

    // ================================================================== ساخت

    public sealed record CreateBotCommand(string Name, string Handle, string? Description) : ICommands<Guid>;

    internal sealed class CreateBotCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IAdminAccess admin) : ICommandHandler<CreateBotCommand, Guid>
    {
        private static readonly Regex HandlePattern = new("^[a-z][a-z0-9_]{2,31}$", RegexOptions.Compiled);

        public async Task<OperationResult<Guid>> Handle(CreateBotCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<Guid>(denied);

            var name = (request.Name ?? string.Empty).Trim();
            var handle = (request.Handle ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();

            if (name.Length is < 2 or > 64)
                return Fail("نام ربات باید بین ۲ تا ۶۴ نویسه باشد.");
            if (!HandlePattern.IsMatch(handle))
                return Fail("شناسه باید با حرف انگلیسی شروع شود و فقط حروف کوچک انگلیسی، عدد و _ داشته باشد (۳ تا ۳۲ نویسه).");
            if (await uow.Bots.AnyAsync(b => b.Handle == handle, ct))
                return Fail("این شناسه قبلاً استفاده شده است.");

            var created = await userManager.CreateBotUserAsync("bot_" + handle, name, ct);
            if (!created.IsSucceded)
                return OperationResult.Failure<Guid>(created);

            var bot = Bot.Create(created.result, handle, request.Description, userManager.UserId!.Value);
            uow.Bots.Add(bot);
            uow.BotMenuNodes.Add(BotMenuNode.CreateRoot(bot.Id, "چه کاری برایتان انجام دهم؟"));
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return OperationResult.Failure<Guid>(new OperationResult().Failed("ذخیره نشد: " + saved.Message));

            return OperationResult.Success(bot.Id);
        }

        private static OperationResult<Guid> Fail(string message)
            => OperationResult.Failure<Guid>(new OperationResult().BadRequest(message));
    }

    // ================================================================== ویرایش مشخصات

    public sealed record UpdateBotCommand(
        Guid Id,
        string Name,
        string? Description,
        bool IsEnabled,
        BotAccessMode AccessMode,
        List<Guid>? AccessList,
        List<BotCommandItem>? Commands) : ICommands<BotAdminDetail>;

    internal sealed class UpdateBotCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin)
        : ICommandHandler<UpdateBotCommand, BotAdminDetail>
    {
        private static readonly Regex CommandPattern = new(@"^/[^\s/]{1,31}$", RegexOptions.Compiled);

        public async Task<OperationResult<BotAdminDetail>> Handle(UpdateBotCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<BotAdminDetail>(denied);

            var bot = await uow.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == request.Id, ct);
            if (bot?.User is null)
                return Fail("ربات یافت نشد.");

            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length is < 2 or > 64)
                return Fail("نام ربات باید بین ۲ تا ۶۴ نویسه باشد.");

            var nodeIds = await uow.BotMenuNodes.Where(n => n.BotId == bot.Id).Select(n => n.Id).ToListAsync(ct);

            var commands = new List<BotCommandItem>();
            foreach (var c in request.Commands ?? new List<BotCommandItem>())
            {
                var command = (c.Command ?? string.Empty).Trim();
                if (!command.StartsWith('/')) command = "/" + command;
                var description = (c.Description ?? string.Empty).Trim();

                if (!CommandPattern.IsMatch(command))
                    return Fail($"دستور «{command}» معتبر نیست؛ با / شروع شود، بدون فاصله و حداکثر ۳۲ نویسه.");
                if (BotAdminGuard.ReservedCommands.Contains(command))
                    return Fail($"«{command}» دستور همیشگی ربات است و لازم نیست اضافه شود.");
                if (commands.Any(x => x.Command == command))
                    return Fail($"دستور «{command}» تکراری است.");
                if (description.Length > 64)
                    return Fail($"توضیح دستور «{command}» حداکثر ۶۴ نویسه است.");
                if (!nodeIds.Contains(c.NodeId))
                    return Fail($"دستور «{command}» به گزینه‌ای از منو وصل نیست.");

                commands.Add(new BotCommandItem(command, description, c.NodeId));
            }

            var accessList = (request.AccessList ?? new List<Guid>()).Distinct().ToList();
            if (request.AccessMode != BotAccessMode.All && accessList.Count == 0)
                return Fail(request.AccessMode == BotAccessMode.Roles
                    ? "حداقل یک نقش انتخاب کنید."
                    : "حداقل یک کاربر انتخاب کنید.");

            bot.User.SetBotName(name);
            bot.SetDescription(request.Description);
            bot.SetEnabled(request.IsEnabled);
            bot.SetAccess(request.AccessMode, BotJson.Serialize(accessList));
            bot.SetCommands(BotJson.Serialize(commands));
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return OperationResult.Failure<BotAdminDetail>(new OperationResult().Failed("ذخیره نشد: " + saved.Message));

            var detail = await BotAdminLoader.LoadAsync(uow, bot.Id, ct);
            return OperationResult.Success(detail!);
        }

        private static OperationResult<BotAdminDetail> Fail(string message)
            => OperationResult.Failure<BotAdminDetail>(new OperationResult().BadRequest(message));
    }

    // ================================================================== ذخیره‌ی منو

    /// <summary>
    /// کل درخت منو یک‌جا ذخیره می‌شود. شناسه‌ی گره‌هایی که از قبل بودند ثابت
    /// می‌ماند تا دکمه‌های پیام‌های قبلی هنوز کار کنند. قبل از ذخیره بررسی می‌شود
    /// که درخت سالم است و هر جریان واقعاً در کد هست و تنظیم‌هایش درست پر شده.
    /// </summary>
    public sealed record SaveBotMenuCommand(Guid Id, List<BotMenuNodeDto> Nodes) : ICommands<BotAdminDetail>;

    internal sealed class SaveBotMenuCommandHandler(IApplicationUnitOfWork uow, IAdminAccess admin, BotPartsRegistry registry)
        : ICommandHandler<SaveBotMenuCommand, BotAdminDetail>
    {
        private const int MaxNodes = 300;

        public async Task<OperationResult<BotAdminDetail>> Handle(SaveBotMenuCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<BotAdminDetail>(denied);

            var bot = await uow.Bots.FirstOrDefaultAsync(b => b.Id == request.Id, ct);
            if (bot is null)
                return Fail("ربات یافت نشد.");

            var nodes = (request.Nodes ?? new List<BotMenuNodeDto>())
                .Select(n => n with { Id = n.Id == Guid.Empty ? Guid.NewGuid() : n.Id })
                .ToList();

            var error = Validate(nodes);
            if (error is not null)
                return Fail(error);

            var existing = await uow.BotMenuNodes.Where(n => n.BotId == bot.Id).ToListAsync(ct);
            var incomingIds = nodes.Select(n => n.Id).ToHashSet();

            // شناسه‌ای که مال ربات دیگری است پذیرفته نمی‌شود.
            var foreign = await uow.BotMenuNodes.AsNoTracking()
                .AnyAsync(n => n.BotId != bot.Id && incomingIds.Contains(n.Id), ct);
            if (foreign)
                return Fail("شناسه‌ی یکی از گزینه‌ها نامعتبر است؛ صفحه را دوباره باز کنید.");

            foreach (var old in existing.Where(o => !incomingIds.Contains(o.Id)))
                uow.BotMenuNodes.Remove(old);

            foreach (var n in nodes)
            {
                var paramsJson = n.Kind == BotMenuNodeKind.Flow && n.Params is { Count: > 0 }
                    ? BotJson.Serialize(n.Params)
                    : null;

                var current = existing.FirstOrDefault(o => o.Id == n.Id);
                if (current is null)
                    uow.BotMenuNodes.Add(BotMenuNode.Create(n.Id, bot.Id, n.ParentId, n.SortOrder, n.Label, n.Text, n.Kind, n.Display, n.FlowKey, paramsJson));
                else
                    current.Update(n.ParentId, n.SortOrder, n.Label, n.Text, n.Kind, n.Display, n.FlowKey, paramsJson);
            }

            // دستورهایی که به گزینه‌ی حذف‌شده اشاره می‌کردند کنار گذاشته می‌شوند.
            var commands = BotJson.Deserialize<List<BotCommandItem>>(bot.CommandsJson) ?? new List<BotCommandItem>();
            var kept = commands.Where(c => incomingIds.Contains(c.NodeId)).ToList();
            if (kept.Count != commands.Count)
                bot.SetCommands(BotJson.Serialize(kept));

            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return OperationResult.Failure<BotAdminDetail>(new OperationResult().Failed("ذخیره نشد: " + saved.Message));

            var detail = await BotAdminLoader.LoadAsync(uow, bot.Id, ct);
            return OperationResult.Success(detail!);
        }

        private string? Validate(List<BotMenuNodeDto> nodes)
        {
            if (nodes.Count == 0)
                return "منو خالی است.";
            if (nodes.Count > MaxNodes)
                return $"منو حداکثر {BotText.Number(MaxNodes)} گزینه می‌تواند داشته باشد.";
            if (nodes.Select(n => n.Id).Distinct().Count() != nodes.Count)
                return "شناسه‌ی تکراری در منو.";

            var roots = nodes.Where(n => n.ParentId is null).ToList();
            if (roots.Count != 1)
                return "منو باید دقیقاً یک ریشه داشته باشد.";
            if (roots[0].Kind != BotMenuNodeKind.Menu)
                return "ریشه‌ی منو باید زیرمنو باشد.";

            var byId = nodes.ToDictionary(n => n.Id);
            foreach (var n in nodes)
            {
                var label = (n.Label ?? string.Empty).Trim();
                if (label.Length == 0)
                    return "متن یکی از گزینه‌ها خالی است.";
                if (label.Length > BotMenuNode.MaxLabelLength)
                    return $"متن گزینه‌ی «{label}» حداکثر {BotText.Number(BotMenuNode.MaxLabelLength)} نویسه است.";
                if (label == BotMenu.BackLabel || label == BotFlowExecution.CancelLabel)
                    return $"«{label}» متن رزروشده است.";
                if ((n.Text ?? string.Empty).Length > BotMenuNode.MaxTextLength)
                    return $"پیام زیرمنوی «{label}» خیلی بلند است.";

                if (n.ParentId is Guid parentId)
                {
                    if (!byId.TryGetValue(parentId, out var parent))
                        return $"والد گزینه‌ی «{label}» پیدا نشد.";
                    if (parent.Kind != BotMenuNodeKind.Menu)
                        return $"«{parent.Label}» زیرمنو نیست و نمی‌تواند گزینه داشته باشد.";

                    // جلوگیری از حلقه
                    var seen = new HashSet<Guid> { n.Id };
                    var cursor = parent;
                    while (cursor is not null)
                    {
                        if (!seen.Add(cursor.Id))
                            return "منو حلقه دارد.";
                        cursor = cursor.ParentId is Guid up && byId.TryGetValue(up, out var next) ? next : null;
                    }
                }

                if (n.Kind == BotMenuNodeKind.Flow)
                {
                    var flow = registry.FindFlow(n.FlowKey);
                    if (flow is null)
                        return $"گزینه‌ی «{label}» از نوع «جریان» است ولی جریانی برایش انتخاب نشده. اگر قرار است زیرش گزینه‌های دیگر بیاید، نوعش را «زیرمنو» بگذارید؛ وگرنه یک جریان انتخاب کنید.";

                    foreach (var p in flow.Params)
                    {
                        string? value = null;
                        var has = n.Params is not null && n.Params.TryGetValue(p.Key, out value) && !string.IsNullOrWhiteSpace(value);
                        if (p.Required && !has)
                            return $"تنظیم «{p.Title}» برای گزینه‌ی «{label}» خالی است.";
                        if (has && p.Options is { Count: > 0 } && !p.Options.Contains(value!))
                            return $"مقدار «{value}» برای «{p.Title}» مجاز نیست.";
                    }
                }
            }

            // متن گزینه‌های هم‌سطح باید یکتا باشد؛ در کیبورد با همین متن پیدا می‌شوند.
            var duplicate = nodes
                .Where(n => n.ParentId is not null)
                .GroupBy(n => (n.ParentId, Label: n.Label.Trim()))
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                return $"گزینه‌ی «{duplicate.Key.Label}» در یک زیرمنو تکراری است.";

            return null;
        }

        private static OperationResult<BotAdminDetail> Fail(string message)
            => OperationResult.Failure<BotAdminDetail>(new OperationResult().BadRequest(message));
    }

    // ================================================================== جریان‌ها و اتصال‌ها

    public sealed record GetBotFlowsQuery : IQuery<List<BotFlowInfo>>;

    internal sealed class GetBotFlowsQueryHandler(BotPartsRegistry registry, IAdminAccess admin)
        : IQueryHandler<GetBotFlowsQuery, List<BotFlowInfo>>
    {
        public Task<OperationResult<List<BotFlowInfo>>> Handle(GetBotFlowsQuery request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return Task.FromResult(OperationResult.Failure<List<BotFlowInfo>>(denied));

            var flows = registry.Flows
                .OrderBy(f => f.Key, StringComparer.Ordinal)
                .Select(f => new BotFlowInfo(f.Key, f.Title, f.Description, f.Params.ToList()))
                .ToList();
            return Task.FromResult(OperationResult.Success(flows));
        }
    }

    public sealed record GetBotConnectorsQuery : IQuery<List<BotConnectorInfo>>;

    internal sealed class GetBotConnectorsQueryHandler(IServiceProvider services, IAdminAccess admin)
        : IQueryHandler<GetBotConnectorsQuery, List<BotConnectorInfo>>
    {
        public Task<OperationResult<List<BotConnectorInfo>>> Handle(GetBotConnectorsQuery request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return Task.FromResult(OperationResult.Failure<List<BotConnectorInfo>>(denied));

            var list = services.GetServices<IBotConnector>()
                .Select(c => new BotConnectorInfo(c.Key, c.Title, c.Description))
                .OrderBy(c => c.Key, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(OperationResult.Success(list));
        }
    }

    public sealed record TestBotConnectorCommand(string Key) : ICommands<BotConnectorTestResult>;

    internal sealed class TestBotConnectorCommandHandler(IServiceProvider services, IAdminAccess admin)
        : ICommandHandler<TestBotConnectorCommand, BotConnectorTestResult>
    {
        public async Task<OperationResult<BotConnectorTestResult>> Handle(TestBotConnectorCommand request, CancellationToken ct)
        {
            if (BotAdminGuard.Check(admin) is { } denied)
                return OperationResult.Failure<BotConnectorTestResult>(denied);

            var connector = services.GetServices<IBotConnector>().FirstOrDefault(c => c.Key == request.Key);
            if (connector is null)
                return OperationResult.Failure<BotConnectorTestResult>(new OperationResult().NotFound("اتصال یافت نشد."));

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(15));
            try
            {
                return OperationResult.Success(await connector.TestAsync(timeout.Token));
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Success(new BotConnectorTestResult(false, "در ۱۵ ثانیه جوابی نیامد."));
            }
            catch (Exception ex)
            {
                return OperationResult.Success(new BotConnectorTestResult(false, ex.Message));
            }
        }
    }
}
