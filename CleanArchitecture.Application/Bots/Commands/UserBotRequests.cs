using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Engine;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Bots.Commands
{
    // ------------------------------------------------------------------ اطلاعات ربات

    public sealed record BotCommandInfo(string Command, string Description);

    public sealed record BotInfoDto(
        Guid BotUserId,
        string Name,
        string Handle,
        string? Description,
        string? Avatar,
        bool IsEnabled,
        bool Started,
        Guid? ConversationId,
        List<BotCommandInfo> Commands,
        List<List<string>>? Keyboard,
        string? Placeholder);

    /// <summary>فرانت هنگام باز کردن چت ربات می‌خواند: شروع شده؟ کیبورد؟ دستورها؟</summary>
    public sealed record GetBotInfoQuery(Guid BotUserId) : IQuery<BotInfoDto>;

    internal sealed class GetBotInfoQueryHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IApplicationRoleManager roles,
        IAdminAccess adminAccess) : IQueryHandler<GetBotInfoQuery, BotInfoDto>
    {
        public async Task<OperationResult<BotInfoDto>> Handle(GetBotInfoQuery request, CancellationToken ct)
        {
            var me = userManager.UserId!.Value;
            var bot = await uow.Bots.AsNoTracking().Include(b => b.User)
                .FirstOrDefaultAsync(b => b.UserId == request.BotUserId, ct);

            if (bot?.User is null || !await BotVisibility.CanUseAsync(bot, me, adminAccess.CurrentUserIsAdmin, roles, ct))
                return OperationResult.Failure<BotInfoDto>(new OperationResult().NotFound("ربات یافت نشد."));

            var session = await uow.BotSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.BotId == bot.Id && s.UserId == me, ct);

            var (a, b) = BotConversations.Pair(me, bot.UserId);
            var conversationId = await uow.Conversation.AsNoTracking()
                .Where(c => c.IsPrivate && c.UserAId == a && c.UserBId == b)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);

            var commands = new List<BotCommandInfo> { new("/شروع", "منوی اصلی") };
            commands.AddRange((BotJson.Deserialize<List<BotCommandItem>>(bot.CommandsJson) ?? new())
                .Select(c => new BotCommandInfo(c.Command, c.Description)));
            commands.Add(new BotCommandInfo("/راهنما", "فهرست دستورها"));
            commands.Add(new BotCommandInfo("/انصراف", "لغو کار نیمه‌تمام"));

            var keyboard = BotJson.Deserialize<BotKeyboardState>(session?.KeyboardJson);

            return OperationResult.Success(new BotInfoDto(
                bot.UserId,
                bot.User.FirsName ?? bot.Handle,
                bot.Handle,
                bot.Description,
                bot.User.Avatar,
                bot.IsEnabled,
                session?.IsStarted ?? false,
                conversationId,
                commands,
                keyboard?.Rows,
                keyboard?.Placeholder));
        }
    }

    // ------------------------------------------------------------------ شروع

    public sealed record StartBotResult(Guid ConversationId, Guid MessageId, DateTime SentAt);

    /// <summary>
    /// «شروع»: گفتگو را (اگر نیست) می‌سازد، پیام «شروع» را از طرف کاربر می‌گذارد و
    /// به موتور خبر می‌دهد. ربات تا قبل از این هیچ پیامی نمی‌فرستد.
    /// </summary>
    public sealed record StartBotCommand(Guid BotUserId) : ICommands<StartBotResult>;

    internal sealed class StartBotCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IApplicationRoleManager roles,
        IAdminAccess adminAccess,
        IBotUpdateQueue queue) : ICommandHandler<StartBotCommand, StartBotResult>
    {
        public async Task<OperationResult<StartBotResult>> Handle(StartBotCommand request, CancellationToken ct)
        {
            var me = userManager.UserId!.Value;
            var bot = await uow.Bots.Include(b => b.User)
                .FirstOrDefaultAsync(b => b.UserId == request.BotUserId, ct);

            if (bot?.User is null || !await BotVisibility.CanUseAsync(bot, me, adminAccess.CurrentUserIsAdmin, roles, ct))
                return OperationResult.Failure<StartBotResult>(new OperationResult().NotFound("ربات یافت نشد."));

            var meUser = await uow.Users.FirstOrDefaultAsync(u => u.Id == me, ct);
            if (meUser is null)
                return OperationResult.Failure<StartBotResult>(new OperationResult().NotFound("کاربر یافت نشد."));

            var conversation = await BotConversations.GetOrCreateAsync(uow, meUser, bot.User, ct);
            if (conversation is null)
                return OperationResult.Failure<StartBotResult>(new OperationResult().Failed("ساخت گفتگو ناموفق بود."));

            var message = conversation.AddMessage("شروع", meUser);
            conversation.LastMessageText = message.Content;
            conversation.LastMessageId = message.Id;
            conversation.LastUserSenderMessageId = me;
            conversation.LastMessageSentAt = message.CreateDate;
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return OperationResult.Failure<StartBotResult>(new OperationResult().Failed("شروع ربات ذخیره نشد: " + saved.Message));

            queue.Enqueue(new BotUpdate(BotUpdateKind.Start, bot.UserId, me, conversation.Id, message.Id));
            return OperationResult.Success(new StartBotResult(conversation.Id, message.Id, message.CreateDate));
        }
    }

    // ------------------------------------------------------------------ دکمه‌ی شیشه‌ای

    public sealed record BotCallbackCommand(Guid MessageId, string Data) : ICommands;

    internal sealed class BotCallbackCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IBotUpdateQueue queue) : ICommandHandler<BotCallbackCommand>
    {
        public async Task<OperationResult> Handle(BotCallbackCommand request, CancellationToken ct)
        {
            var me = userManager.UserId!.Value;
            if (string.IsNullOrWhiteSpace(request.Data) || request.Data.Length > 100)
                return new OperationResult().BadRequest("دکمه معتبر نیست.");

            // پیام باید در گفتگوی خصوصیِ همین کاربر باشد و دکمه واقعاً زیرش باشد؛
            // وگرنه کسی می‌تواند با ساختن داده‌ی دلخواه، مراحل جریان را دور بزند.
            var message = await uow.Messages.AsNoTracking()
                .Where(m => m.Id == request.MessageId
                            && !m.Deleted
                            && m.ConversationId != null
                            && m.Conversation!.IsPrivate
                            && m.Conversation.Users.Any(u => u.UserId == me))
                .Select(m => new { ConversationId = m.ConversationId!.Value, m.CreatedByUserId, m.ReplyMarkup })
                .FirstOrDefaultAsync(ct);

            if (message?.CreatedByUserId is not Guid botUserId)
                return new OperationResult().NotFound("پیام یافت نشد.");

            var isBot = await uow.Bots.AsNoTracking().AnyAsync(b => b.UserId == botUserId, ct);
            var markup = BotReplyMarkup.FromJson(message.ReplyMarkup);
            if (!isBot || markup is null || !markup.ContainsData(request.Data))
                return new OperationResult().BadRequest("این دکمه دیگر فعال نیست.");

            queue.Enqueue(new BotUpdate(BotUpdateKind.Callback, botUserId, me, message.ConversationId, request.MessageId, null, request.Data));
            return new OperationResult().succedded();
        }
    }

    // ------------------------------------------------------------------ کمکی

    internal static class BotConversations
    {
        public static (Guid A, Guid B) Pair(Guid x, Guid y)
            => x.CompareTo(y) <= 0 ? (x, y) : (y, x);

        /// <summary>
        /// گفتگوی خصوصی کاربر و ربات؛ اگر نیست ساخته می‌شود. مسابقه‌ی دو درخواست
        /// هم‌زمان با ایندکس یکتای جفت کاربران مدیریت می‌شود، مثل CreateConversation.
        /// </summary>
        public static async Task<Conversation?> GetOrCreateAsync(
            IApplicationUnitOfWork uow,
            CleanArchitecture.Domain.Entities.Identity.User me,
            CleanArchitecture.Domain.Entities.Identity.User botUser,
            CancellationToken ct)
        {
            var (a, b) = Pair(me.Id, botUser.Id);
            var existing = await uow.Conversation.Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.IsPrivate && c.UserAId == a && c.UserBId == b, ct);
            if (existing is not null)
                return existing;

            var created = Conversation.Create(me, botUser);
            uow.Conversation.Add(created);
            // واحد کار خطای دیتابیس را نمی‌اندازد؛ نتیجه را نگاه می‌کنیم. اگر نشد
            // (معمولاً درخواست هم‌زمانی زودتر ساخته)، همان گفتگو را برمی‌گردانیم.
            var saved = await uow.SaveChangesAsync(ct);
            if (saved.IsSuccess)
                return created;

            {
                foreach (var member in created.Users)
                    uow.ConversationUser.Entry(member).State = EntityState.Detached;
                uow.Conversation.Entry(created).State = EntityState.Detached;
                return await uow.Conversation.Include(c => c.Users)
                    .FirstOrDefaultAsync(c => c.IsPrivate && c.UserAId == a && c.UserBId == b, ct);
            }
        }
    }
}
