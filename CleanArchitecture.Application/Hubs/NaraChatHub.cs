using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace CleanArchitecture.Application.Hubs
{
    public class NaraHub(IApplicationUserManager usermanager, IApplicationUnitOfWork uow
        ,ILogger<NaraHub> looger,IHttpContextAccessor httpContextAccessor,
        HubConnectionRegistry connections) : Hub<IChatHubClient>, IChatHubServer
    {
        private static readonly ConcurrentDictionary<Guid, UserDto> _onlineUsers = new ConcurrentDictionary<Guid, UserDto>();
        private static readonly object _presenceLock = new();
        private readonly IApplicationUserManager usermanager = usermanager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly ILogger<NaraHub> _loger = looger;
        private readonly IHttpContextAccessor _httpContextAccessor= httpContextAccessor;

        public override async Task OnConnectedAsync()
        {
            // برای بستن فوری اتصال وقتی حساب غیرفعال یا از همه‌ی دستگاه‌ها خارج می‌شود.
            if (Guid.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var connectedUserId))
                connections.Add(connectedUserId, Context);

#if DEBUG
            var token = Context.GetHttpContext().Request.Query.FirstOrDefault(x =>
            x.Key == "access_token");
            var existUser = Context.User.Identity.IsAuthenticated;
            var userIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                var userId = userIdClaim.Value;
                var channelIds = await _uow.Channels
                 .Where(cu => cu.Members.Any(x => x.UserId == Guid.Parse(userId)))
                 .Select(cu => cu.Id)
                 .ToListAsync();
                var groups=await _uow.Conversation.Where(x=>x.IsPrivate==false &&
                x.Users.Any(x=>x.UserId == Guid.Parse(userId))).ToListAsync();
                foreach (var group in groups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, group.Id.ToString());
                }
                foreach (var channelId in channelIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());
                }
            }


#else
            try
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            _loger.LogError("HttpContext is null in OnConnectedAsync (Release).");
            return;
        }

        // 🚩 توکن از querystring دقیقا مثل چیزی که فرانت می‌فرسته
        var rawToken = httpContext.Request.Query["access_token"].ToString();
        // توکن هرگز لاگ نشود: ۷ روز اعتبار دارد و لاگ‌ها در دیتابیس Serilog می‌ماند.

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            _loger.LogError("access_token query is empty in Release.");
            return;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(rawToken);

        var userId = jwtToken.Claims.First(x => x.Type == "sub").Value;
        _loger.LogError("my user id is {userid}", userId);

        if (!string.IsNullOrEmpty(userId))
        {
            var guid = Guid.Parse(userId);

            var channelIds = await _uow.Channels
                .Where(cu => cu.Members.Any(x => x.UserId == guid))
                .Select(cu => cu.Id)
                .ToListAsync();

                     var groups=await _uow.Conversation.Where(x=>x.IsPrivate==false &&
                x.Users.Any(x=>x.UserId == Guid.Parse(userId))).ToListAsync();
                foreach (var group in groups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, group.Id.ToString());
                }

            foreach (var channelId in channelIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());
            }

            _loger.LogWarning("UserId Id {userId}", userId);
            await SetUserOnline(new UserDto(guid, string.Empty));
        }
    }
    catch (Exception ex)
    {
        _loger.LogError(ex, "Error in OnConnectedAsync (Release).");
    }
#endif


        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            connections.Remove(Context.ConnectionId);
            try
            {
                // کاربر فقط وقتی آفلاین حساب می‌شود که آخرین اتصالش قطع شود.
                //
                // قبلاً با قطع شدن هر اتصالی، کل کاربر آفلاین می‌شد. کسی که دو
                // تب یا دو دستگاه داشت، یا اتصالش خودکار دوباره وصل شده بود و
                // قطعی اتصال قدیمی‌اش دیرتر به سرور رسید، آفلاین نشان داده می‌شد
                // در حالی که آنلاین بود، و «آخرین بازدید» روی زمان همان قطعی
                // قدیمی می‌ماند.
                var (userId, wentOffline) = RemoveConnection(Context.ConnectionId);

                if (userId is null || !wentOffline)
                    return;

                await MarkOfflineAsync(userId.Value);
            }
            catch (Exception e)
            {
                _loger.LogError($" u have exception on disconnected : {e.Message}");
            }
        }
        public async Task SetUserOnline(UserDto user)
        {
            var connectionId = Context.ConnectionId;
            var userId = CurrentUserId() ?? user.id;
            bool isNewlyOnline;
            List<UserDto> snapshot;

            // همه‌ی تغییرات فهرست آنلاین‌ها زیر یک قفل انجام می‌شود تا وصل شدن
            // و قطع شدن هم‌زمانِ یک کاربر (مثلاً اتصال مجدد خودکار) همدیگر را
            // خراب نکنند. این کارها کم‌تکرارند، پس قفل سراسری هزینه‌ای ندارد.
            lock (_presenceLock)
            {
                if (_onlineUsers.TryGetValue(userId, out var existing))
                {
                    if (!existing.ConnectionIds.Contains(connectionId))
                        existing.ConnectionIds.Add(connectionId);

                    isNewlyOnline = existing.ConnectionIds.Count == 1;
                }
                else
                {
                    _onlineUsers[userId] = new UserDto(userId, user.Name)
                    {
                        ConnectionIds = { connectionId }
                    };
                    isNewlyOnline = true;
                }

                snapshot = _onlineUsers.Values.ToList();
            }

            await Clients.Caller.OnlineUserList(snapshot);

            if (isNewlyOnline)
            {
                await Clients.Others.UserConnected(user);
                await Clients.Others.UserIsOnline(userId);
            }
        }
        public async Task MessageSeen(MessageSeenDto messagesForSeen)
        {
            var conversation = await _uow.Conversation
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Id == messagesForSeen.ConversationId);

            if (conversation is null) return;

            // «من» از توکن خوانده می‌شود، نه از MyId که کلاینت می‌فرستد؛
            // وگرنه هر کلاینتی می‌توانست شمارنده‌ی کاربر دیگری را صفر کند.
            if (CurrentUserId() is not Guid myId) return;

            // ۱ — شمارنده‌ی خودم را صفر کن. بی‌قید و شرط.
            var me = conversation.Users.FirstOrDefault(x => x.UserId == myId);

            // کسی که عضو این گفتگو نیست نباید تیک خوانده‌شدن برای آن بفرستد
            if (me is null) return;
            if (me is not null && me.UnreadCount > 0)
            {
                me.EmptyCount();
                await _uow.SaveChangesAsync();
            }

            // ۲ — تیک آبی فقط اگر طرف مقابل آنلاین باشد
            if (!_onlineUsers.ContainsKey(messagesForSeen.UserId)) return;

            var exists = await _uow.Messages
                .AnyAsync(x => messagesForSeen.messageId.Contains(x.Id));

            if (exists)
            {
                await Clients.User(messagesForSeen.UserId.ToString())
                    .MessagedSeenReceived(messagesForSeen.messageId);
            }
        }

        public async Task SetUserOffline(UserDto user)
        {
            // فقط همین اتصال کم می‌شود؛ اگر کاربر تب یا دستگاه دیگری باز دارد
            // آنلاین می‌ماند.
            var (userId, wentOffline) = RemoveConnection(Context.ConnectionId);

            if (userId is null || !wentOffline)
                return;

            await MarkOfflineAsync(userId.Value);
        }

        /// <summary>
        /// یک اتصال را از فهرست آنلاین‌ها حذف می‌کند. اگر آخرین اتصال کاربر بود،
        /// خود کاربر هم حذف می‌شود و wentOffline برابر true است.
        /// </summary>
        private static (Guid? UserId, bool WentOffline) RemoveConnection(string connectionId)
        {
            lock (_presenceLock)
            {
                var entry = _onlineUsers.Values
                    .FirstOrDefault(u => u.ConnectionIds.Contains(connectionId));

                if (entry is null)
                    return (null, false);

                // ConcurrentBag حذف یک مورد مشخص را پشتیبانی نمی‌کند، پس بدون
                // این اتصال از نو ساخته می‌شود.
                entry.ConnectionIds = new ConcurrentBag<string>(
                    entry.ConnectionIds.Where(c => c != connectionId));

                if (!entry.ConnectionIds.IsEmpty)
                    return (entry.id, false);

                _onlineUsers.TryRemove(entry.id, out _);
                return (entry.id, true);
            }
        }

        /// <summary>زمان آخرین بازدید را ذخیره و آفلاین شدن را به بقیه اعلام می‌کند.</summary>
        private async Task MarkOfflineAsync(Guid userId)
        {
            var dbUser = await usermanager.GetUserBy(userId);

            if (dbUser is null)
            {
                _loger.LogError("User is Empty!!");
                return;
            }

            dbUser.UpdatelastLoginDate();
            await _uow.SaveChangesAsync();

            await Clients.Others.UserIsOffline(userId);
            await Clients.Others.SetLastSeenUser(
                new LastSeenModelDto(userId, dbUser.LastLoginDate!.Value.DateTime));
        }

        public async Task TypingReaction(TypingReactionDto reaction)
        {
            await Clients.User(reaction.UserId.ToString()).ReceivedReactions(new TypingReactionDto(reaction.MyUserId, reaction.UserId, reaction.MessageType));

        }
        public async Task BlockedUser(BlockDto block)
        {
            await Clients.User(block.UserId.ToString()).BlockUser(new BlockDto(block.UserId,block.IsBlocked));

        }
        public async Task ReceiveReaction(MessageReactionDto reactionDto)
        {
            await Clients.User(reactionDto.OtherUserId.ToString()).ReceivedEmojiReact( reactionDto);

        }
        public Task Ping()
        {
            return Task.CompletedTask;
        }
        public static IReadOnlyList<string> GetUserConnections(Guid userId)
        {
            if (_onlineUsers.TryGetValue(userId, out var user))
            {
            
                return user.ConnectionIds.ToList();
            }

            return Array.Empty<string>();
        }
        public async Task GetMissedMessages(ChatMessageDto Lastmessage)
        {
            // هویت کاربر فقط از توکن احراز هویت خوانده می‌شود، نه از چیزی که
            // کلاینت می‌فرستد.
            //
            // نسخه‌ی قبلی «من» را از روی پیام ورودی حدس می‌زد: هر عضوی از آن
            // گفتگو که فرستنده نبود. در چت خصوصی تصادفاً درست درمی‌آمد، ولی
            // وقتی آخرین پیام دریافتی مال یک گروه بود، یک عضو دلخواه گروه به‌جای
            // کاربر انتخاب می‌شد و پیام‌های گفتگوهای او — از جمله چت‌های
            // خصوصی‌اش با دیگران — برای این کاربر فرستاده می‌شد. شاخه‌ی دیگر هم
            // UserId را مستقیم از کلاینت می‌گرفت، یعنی هر کلاینتی می‌توانست با
            // فرستادن شناسه‌ی دیگری پیام‌های او را بخواند.
            if (CurrentUserId() is not Guid myId)
                return;

            // نقطه‌ی شروع. از پیام ورودی فقط شناسه‌اش استفاده می‌شود، و فقط اگر
            // در گفتگویی باشد که این کاربر عضوش است؛ تاریخ هم از دیتابیس
            // خوانده می‌شود نه از کلاینت.
            DateTime? since = null;

            if (Lastmessage is not null && Lastmessage.Id != Guid.Empty)
            {
                since = await _uow.Messages.AsNoTracking()
                    .Where(x => x.Id == Lastmessage.Id
                             && x.Conversation!.Users.Any(u => u.UserId == myId))
                    .Select(x => (DateTime?)x.CreateDate)
                    .FirstOrDefaultAsync();
            }

            // اگر نقطه‌ی شروع معتبری نیامد، آخرین پیامی که این کاربر دیده.
            since ??= await _uow.Messages.AsNoTracking()
                .Where(x => x.Conversation!.Users.Any(u => u.UserId == myId)
                         && x.CreatedByUserId != myId
                         && x.Seen)
                .OrderByDescending(x => x.CreateDate)
                .Select(x => (DateTime?)x.CreateDate)
                .FirstOrDefaultAsync();

            var from = since ?? DateTime.MinValue;

            var missedMessage = await _uow.Messages.AsNoTracking()
                .Where(x => x.Conversation!.Users.Any(u => u.UserId == myId)
                         && x.CreatedByUserId != myId
                         && x.CreateDate > from)
                // اگر بیش از ۵۰۰ پیام جا مانده، جدیدترین‌ها فرستاده می‌شوند تا
                // آخرین پیام هر گفتگو در لیست درست باشد.
                .OrderByDescending(x => x.CreateDate)
                .Take(500)
                .Select(x => new ChatMessageDto
                {
                    Id = x.Id,
                    ScopeId = x.ConversationId!.Value,
                    ConversationType = x.Conversation!.IsPrivate
                        ? ConversationTyped.Private
                        : ConversationTyped.group,
                    UserId = x.CreatedByUserId!.Value,
                    Content = x.Content,
                    IsMine = false,
                    IsSeen = x.Seen,
                    SendAt = x.CreateDate,
                    ParentId = x.ParentMessageId,
                    SenderName = "",
                    Type = (int)x.MessageType,
                    ForwardedFromName = x.ForwardedFromName,
                    ReplyMarkup = x.ReplyMarkup,
                    FileContent = x.ChatFiles.Select(cf => new ChatFilesDto
                    {
                        FileId = cf.Id,
                        FileName = cf.FileName,
                        FileSize = cf.FileSize.ToString()
                    }).FirstOrDefault(),
                })
                .ToListAsync();

            if (missedMessage.Count > 0)
                await Clients.Caller.GetMissedMessages(
                    missedMessage.OrderBy(x => x.SendAt).ToList());
        }

        /// <summary>
        /// شناسه‌ی کاربرِ این اتصال، از توکن احراز هویت. توکن شناسه را در sub
        /// می‌گذارد و JwtBearer به‌صورت پیش‌فرض آن را به NameIdentifier نگاشت
        /// می‌کند؛ اگر روزی این نگاشت خاموش شد، sub هم خوانده می‌شود.
        /// </summary>
        private Guid? CurrentUserId()
        {
            var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
