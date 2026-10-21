using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Flows;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Bots;
using CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Engine
{
    public interface IBotEngine
    {
        Task HandleAsync(BotUpdate update, CancellationToken cancellationToken);
    }

    /// <summary>
    /// موتور ربات: به هر اتفاق (شروع، متن، دکمه، فایل) جواب می‌دهد.
    ///
    /// ترتیب تصمیم برای متن:
    ///   ۱. انصراف، شروع، راهنما و دستورهای «/»
    ///   ۲. اگر جریانی نیمه‌تمام است، متن جواب سؤال آن است
    ///   ۳. اگر منوی کیبوردی باز است، متن یکی از دکمه‌های آن است
    ///   ۴. وگرنه «متوجه نشدم» و منوی اصلی
    /// </summary>
    internal sealed class BotEngine(
        IApplicationUnitOfWork uow,
        BotMessenger messenger,
        BotPartsRegistry registry,
        IServiceProvider services,
        IApplicationRoleManager roles,
        IAdminAccess adminAccess,
        ILogger<BotEngine> logger) : IBotEngine
    {
        private static readonly TimeSpan FlowLifetime = TimeSpan.FromHours(24);
        private static readonly string[] CancelWords = { "/انصراف", "انصراف", "/cancel" };
        private static readonly string[] StartWords = { "/شروع", "/start" };
        private static readonly string[] HelpWords = { "/راهنما", "/help" };

        public async Task HandleAsync(BotUpdate update, CancellationToken ct)
        {
            var bot = await uow.Bots.Include(b => b.User).FirstOrDefaultAsync(b => b.UserId == update.BotUserId, ct);
            if (bot?.User is null)
                return;

            var conversation = await uow.Conversation
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == update.ConversationId && c.IsPrivate, ct);
            var membership = conversation?.Users.FirstOrDefault(u => u.UserId == update.UserId);
            if (conversation is null || membership is null || !conversation.Users.Any(u => u.UserId == bot.UserId))
                return;

            var human = await uow.Users.FirstOrDefaultAsync(u => u.Id == update.UserId, ct);
            if (human is null || human.IsBot)
                return;

            var session = await uow.BotSessions.FirstOrDefaultAsync(s => s.BotId == bot.Id && s.UserId == human.Id, ct);
            if (session is null)
            {
                session = BotSession.Create(bot.Id, human.Id);
                uow.BotSessions.Add(session);
            }

            var nodes = await uow.BotMenuNodes.Where(n => n.BotId == bot.Id).ToListAsync(ct);
            var turn = new BotTurn(bot, bot.User, human, conversation, membership, session, new BotMenu(nodes), ct);

            if (update.Kind != BotUpdateKind.Callback && update.MessageId is Guid userMessageId)
                await messenger.MarkSeenAsync(turn, userMessageId);

            if (!await BotVisibility.CanUseAsync(bot, human.Id, adminAccess.IsAdmin(human.Id), roles, ct))
            {
                await messenger.SendAsync(turn, "این ربات در حال حاضر برای شما در دسترس نیست.");
                return;
            }

            switch (update.Kind)
            {
                case BotUpdateKind.Start:
                    await StartAsync(turn);
                    break;

                case BotUpdateKind.Text:
                    if (!session.IsStarted) await StartAsync(turn);
                    else await OnTextAsync(turn, (update.Text ?? string.Empty).Trim());
                    break;

                case BotUpdateKind.Callback:
                    if (session.IsStarted && update.MessageId is Guid messageId && update.Data is string data)
                        await OnCallbackAsync(turn, messageId, data);
                    break;

                case BotUpdateKind.File:
                    if (!session.IsStarted) await StartAsync(turn);
                    else await messenger.SendAsync(turn, session.HasFlow
                        ? "در این مرحله فایل لازم نیست؛ لطفاً جواب را بنویسید."
                        : "این ربات فعلاً فایل دریافت نمی‌کند.");
                    break;
            }

            session.Touch();
            await uow.SaveOrThrowAsync(ct);
            await SyncKeyboardAsync(turn);
        }

        // ------------------------------------------------------------------ شروع و منو

        private async Task StartAsync(BotTurn turn)
        {
            await TidyPendingAsync(turn, "لغو شد");
            turn.Session.Start();

            var root = turn.Menu.Root;
            if (root is null)
            {
                await messenger.SendAsync(turn, "این ربات هنوز آماده نیست.");
                return;
            }

            await ShowMenuAsync(turn, root, sourceMessageId: null);
        }

        /// <summary>
        /// یک زیرمنو را نشان می‌دهد. شیشه‌ای: اگر از دکمه‌ی پیام قبلی آمده، همان پیام
        /// عوض می‌شود؛ وگرنه پیام تازه. کیبوردی: پیام متن زیرمنو و کیبورد گزینه‌ها.
        /// </summary>
        private async Task ShowMenuAsync(BotTurn turn, BotMenuNode menu, Guid? sourceMessageId)
        {
            var text = string.IsNullOrWhiteSpace(menu.Text) ? menu.Label : menu.Text!;

            if (menu.Display == BotMenuDisplay.Keyboard)
            {
                turn.Session.SetMenuNode(menu.Id);
                if (sourceMessageId is Guid source)
                    await messenger.EditAsync(turn, source, null, PathNote(turn, menu), markEdited: false);
                await messenger.SendAsync(turn, text);
                return;
            }

            var markup = turn.Menu.InlineMarkup(menu);
            if (sourceMessageId is Guid src)
                await messenger.EditAsync(turn, src, text, markup, markEdited: false);
            else
                await messenger.SendAsync(turn, text, markup);
        }

        /// <summary>گزینه‌ی منو: زیرمنو را نشان می‌دهد یا جریانش را شروع می‌کند.</summary>
        private async Task OpenNodeAsync(BotTurn turn, BotMenuNode node, Guid? sourceMessageId)
        {
            if (node.Kind == BotMenuNodeKind.Menu)
            {
                await ShowMenuAsync(turn, node, sourceMessageId);
                return;
            }

            if (turn.Session.HasFlow)
            {
                await TidyPendingAsync(turn, "لغو شد");
                turn.Session.ClearFlow();
            }

            // دکمه‌های پیام منو برداشته می‌شوند و مسیر انتخاب زیرش می‌ماند.
            if (sourceMessageId is Guid source)
                await messenger.EditAsync(turn, source, null, PathNote(turn, node), markEdited: false);

            await StartFlowAsync(turn, node.FlowKey, node.ParamsJson);
        }

        private static BotReplyMarkup PathNote(BotTurn turn, BotMenuNode node)
            => new() { Note = turn.Menu.Path(node), NoteTone = "muted" };

        private async Task ShowRootWithTextAsync(BotTurn turn, string text)
        {
            var root = turn.Menu.Root;
            if (root is null)
            {
                await messenger.SendAsync(turn, text);
                return;
            }

            if (root.Display == BotMenuDisplay.Keyboard)
            {
                turn.Session.SetMenuNode(root.Id);
                await messenger.SendAsync(turn, text);
            }
            else
            {
                turn.Session.SetMenuNode(null);
                await messenger.SendAsync(turn, text, turn.Menu.InlineMarkup(root));
            }
        }

        // ------------------------------------------------------------------ متن

        private async Task OnTextAsync(BotTurn turn, string text)
        {
            var session = turn.Session;

            if (CancelWords.Contains(text))
            {
                var hadFlow = session.HasFlow;
                await TidyPendingAsync(turn, "لغو شد");
                session.ClearFlow();
                await ShowRootWithTextAsync(turn, hadFlow
                    ? "کار نیمه‌تمام لغو شد. چه کار دیگری برایتان انجام دهم؟"
                    : "کاری در جریان نبود. چه کاری برایتان انجام دهم؟");
                return;
            }

            if (StartWords.Contains(text))
            {
                await TidyPendingAsync(turn, "لغو شد");
                session.ClearFlow();
                var root = turn.Menu.Root;
                if (root is not null)
                    await ShowMenuAsync(turn, root, null);
                return;
            }

            if (HelpWords.Contains(text))
            {
                await messenger.SendAsync(turn, HelpText(turn));
                return;
            }

            if (text.StartsWith('/'))
            {
                var command = Commands(turn).FirstOrDefault(c => string.Equals(c.Command, text, StringComparison.Ordinal));
                var target = command is null ? null : turn.Menu.Find(command.NodeId);
                if (target is null)
                {
                    await messenger.SendAsync(turn, "این دستور را نمی‌شناسم. برای فهرست دستورها «/راهنما» را بزنید.");
                    return;
                }

                await OpenNodeAsync(turn, target, null);
                return;
            }

            if (session.HasFlow)
            {
                await ContinueFlowAsync(turn, new BotFlowInput(text, null, null));
                return;
            }

            if (session.MenuNodeId is Guid keyboardMenuId && turn.Menu.Find(keyboardMenuId) is { } keyboardMenu)
            {
                if (text == BotMenu.BackLabel)
                {
                    var parent = turn.Menu.Parent(keyboardMenu);
                    if (parent is not null)
                    {
                        if (parent.Display != BotMenuDisplay.Keyboard)
                            session.SetMenuNode(null);
                        await ShowMenuAsync(turn, parent, null);
                        return;
                    }
                }

                var child = turn.Menu.ChildByLabel(keyboardMenu.Id, text);
                if (child is not null)
                {
                    await OpenNodeAsync(turn, child, null);
                    return;
                }
            }

            await ShowRootWithTextAsync(turn, "متوجه نشدم. یکی از گزینه‌ها را انتخاب کنید یا «/» را بزنید.");
        }

        // ------------------------------------------------------------------ دکمه‌ی شیشه‌ای

        private async Task OnCallbackAsync(BotTurn turn, Guid messageId, string data)
        {
            if (BotMenu.TryParseNodeData(data, out var nodeId))
            {
                var node = turn.Menu.Find(nodeId);
                if (node is null)
                {
                    await messenger.EditAsync(turn, messageId, null,
                        new BotReplyMarkup { Note = "این منو عوض شده است؛ «/شروع» را بزنید.", NoteTone = "muted" }, markEdited: false);
                    return;
                }

                await OpenNodeAsync(turn, node, messageId);
                return;
            }

            if (data.StartsWith("a:", StringComparison.Ordinal))
            {
                if (!turn.Session.HasFlow)
                {
                    await messenger.EditAsync(turn, messageId, null,
                        new BotReplyMarkup { Note = "این گزینه دیگر فعال نیست.", NoteTone = "muted" }, markEdited: false);
                    return;
                }

                await ContinueFlowAsync(turn, new BotFlowInput(null, data, messageId));
            }
        }

        // ------------------------------------------------------------------ جریان

        private async Task StartFlowAsync(BotTurn turn, string? flowKey, string? paramsJson)
        {
            var descriptor = registry.FindFlow(flowKey);
            if (descriptor is null)
            {
                logger.LogWarning("Bot {Bot}: flow '{Flow}' not found", turn.Bot.Handle, flowKey);
                await messenger.SendAsync(turn, "این بخش موقتاً در دسترس نیست.");
                return;
            }

            turn.Session.StartFlow(descriptor.Key, paramsJson);
            await RunFlowAsync(turn, descriptor, new BotFlowState(), input: null, firstRun: true);
        }

        private async Task ContinueFlowAsync(BotTurn turn, BotFlowInput input)
        {
            var session = turn.Session;
            var descriptor = registry.FindFlow(session.FlowKey);
            if (descriptor is null)
            {
                session.ClearFlow();
                await ShowRootWithTextAsync(turn, "این بخش موقتاً در دسترس نیست.");
                return;
            }

            if (session.FlowStartedAt is DateTime started && DateTime.Now - started > FlowLifetime)
            {
                await TidyPendingAsync(turn, "زمانش تمام شد");
                session.ClearFlow();
                await ShowRootWithTextAsync(turn, "زمان این کار تمام شده بود؛ لطفاً دوباره از منو شروع کنید.");
                return;
            }

            var state = BotJson.Deserialize<BotFlowState>(session.FlowStateJson) ?? new BotFlowState();
            await RunFlowAsync(turn, descriptor, state, input, firstRun: false);
        }

        private async Task RunFlowAsync(BotTurn turn, BotFlowDescriptor descriptor, BotFlowState state, BotFlowInput? input, bool firstRun)
        {
            var session = turn.Session;
            var parameters = BotJson.Deserialize<Dictionary<string, string>>(session.FlowParamsJson) ?? new Dictionary<string, string>();
            var user = new BotFlowUser(
                turn.Human.Id,
                $"{turn.Human.FirsName} {turn.Human.LastName}".Trim(),
                turn.Human.NationalCode,
                turn.Human.PhoneNumber,
                turn.Human.Address?.City,
                turn.Human.FirsName);

            var execution = new BotFlowExecution(turn, messenger, state, input, firstRun);
            var context = new BotFlowContext(execution, user, parameters, turn.Bot.Id);

            IBotFlow flow;
            try
            {
                flow = (IBotFlow)ActivatorUtilities.CreateInstance(services, descriptor.Type);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bot flow {Flow} could not be created", descriptor.Key);
                session.ClearFlow();
                await messenger.SendAsync(turn, "این بخش موقتاً در دسترس نیست.");
                return;
            }

            var outcome = await execution.RunAsync(flow, context);
            switch (outcome)
            {
                case BotFlowOutcome.Waiting:
                    session.SaveFlowState(BotJson.Serialize(state));
                    break;

                case BotFlowOutcome.Stale:
                    if (input?.MessageId is Guid staleMessage)
                        await messenger.EditAsync(turn, staleMessage, null,
                            new BotReplyMarkup { Note = "این گزینه دیگر فعال نیست.", NoteTone = "muted" }, markEdited: false);
                    break;

                case BotFlowOutcome.Completed:
                    session.ClearFlow();
                    break;

                case BotFlowOutcome.ReplayMismatch:
                    await TidyPendingAsync(turn, "لغو شد");
                    session.ClearFlow();
                    await ShowRootWithTextAsync(turn, "این بخش به‌روز شده است؛ لطفاً دوباره از منو شروع کنید.");
                    break;

                case BotFlowOutcome.Failed:
                    logger.LogError(execution.Error, "Bot flow {Flow} failed for user {User}", descriptor.Key, turn.Human.Id);
                    await TidyPendingAsync(turn, "ناتمام ماند");
                    session.ClearFlow();
                    await ShowRootWithTextAsync(turn, "متأسفانه خطایی پیش آمد. لطفاً کمی بعد دوباره امتحان کنید.");
                    break;
            }
        }

        /// <summary>دکمه‌های سؤالِ بی‌جواب جریان را برمی‌دارد و یادداشتی زیرش می‌گذارد.</summary>
        private async Task TidyPendingAsync(BotTurn turn, string note)
        {
            if (!turn.Session.HasFlow)
                return;

            var state = BotJson.Deserialize<BotFlowState>(turn.Session.FlowStateJson);
            if (state?.PendingMessageId is Guid pending)
                await messenger.EditAsync(turn, pending, null,
                    new BotReplyMarkup { Card = state.PendingCard, Note = note, NoteTone = "muted" }, markEdited: false);
        }

        // ------------------------------------------------------------------ کیبورد

        /// <summary>
        /// کیبوردی که کاربر باید ببیند: پیشنهادهای سؤال جریان، یا فقط «/انصراف» وسط
        /// جریان، یا گزینه‌های منوی کیبوردی. اگر عوض شده باشد به کاربر خبر می‌دهد.
        /// </summary>
        private async Task SyncKeyboardAsync(BotTurn turn)
        {
            var session = turn.Session;
            BotKeyboardState? keyboard = null;

            if (session.HasFlow)
            {
                var state = BotJson.Deserialize<BotFlowState>(session.FlowStateJson);
                keyboard = new BotKeyboardState(
                    state?.PendingKeyboard ?? new List<List<string>> { new() { BotFlowExecution.CancelLabel } },
                    state?.PendingPlaceholder);
            }
            else if (session.MenuNodeId is Guid menuId && turn.Menu.Find(menuId) is { } menu && turn.Menu.IsKeyboard(menu))
            {
                keyboard = new BotKeyboardState(turn.Menu.KeyboardRows(menu), null);
            }

            var json = keyboard is null ? null : BotJson.Serialize(keyboard);
            if (json == session.KeyboardJson)
                return;

            session.SetKeyboard(json);
            await uow.SaveOrThrowAsync(turn.CancellationToken);
            await messenger.PushStateAsync(turn, new BotStateDto(turn.BotUser.Id, session.IsStarted, keyboard?.Rows, keyboard?.Placeholder));
        }

        // ------------------------------------------------------------------ راهنما

        private static List<BotCommandItem> Commands(BotTurn turn)
            => BotJson.Deserialize<List<BotCommandItem>>(turn.Bot.CommandsJson) ?? new List<BotCommandItem>();

        private static string HelpText(BotTurn turn)
        {
            var lines = new List<string> { "دستورهای این ربات:", "/شروع — منوی اصلی" };
            lines.AddRange(Commands(turn).Select(c => $"{c.Command} — {c.Description}"));
            lines.Add("/انصراف — لغو کار نیمه‌تمام");
            return string.Join("\n", lines);
        }
    }
}
