using CleanArchitecture.Application.Abstraction.Authentication;
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
        ,ILogger<NaraHub> looger,IHttpContextAccessor httpContextAccessor) : Hub<IChatHubClient>, IChatHubServer
    {
        private static readonly ConcurrentDictionary<Guid, UserDto> _onlineUsers = new ConcurrentDictionary<Guid, UserDto>();
        private readonly IApplicationUserManager usermanager = usermanager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly ILogger<NaraHub> _loger = looger;
        private readonly IHttpContextAccessor _httpContextAccessor= httpContextAccessor;

        public override async Task OnConnectedAsync()
        {
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
        _loger.LogWarning("token (query access_token) is {token}", rawToken);

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
            try
            {
                var connectionId = Context.ConnectionId;


                var user = _onlineUsers.Values.FirstOrDefault(u =>
                    u.ConnectionIds.Contains(connectionId));
                if (user == null)
                {
                    return;
                }
                var connectUser = await usermanager.GetUserBy(user.id);
                if (connectUser != null)
                {
                    connectUser.UpdatelastLoginDate();
                    _onlineUsers.Remove(connectUser.Id, out var userd);

                    await Clients.Others.UserIsOffline(connectUser.Id);
                    await Clients.Others.SetLastSeenUser(new LastSeenModelDto(connectUser.Id, connectUser.LastLoginDate!.Value.DateTime));
                    await _uow.SaveChangesAsync();

                }

                else
                {
                    _loger.LogError("User is Empty!!");
                }
            }
            catch (Exception e)
            {

                _loger.LogError($" u have exception on disconnected : {e.Message}");
            }

        }
        public async Task SetUserOnline(UserDto user)
        {
            var connectionId = Context.ConnectionId;
            bool isNewlyOnline = false;

            _onlineUsers.AddOrUpdate(
                user.id,
                (userId) =>
                {

                    var newUser = new UserDto(user.id, user.Name)
                    {
                        ConnectionIds = { connectionId }
                    };
                    isNewlyOnline = true;
                    return newUser;
                },
                (userId, existingUser) =>
                {

                    if (!existingUser.ConnectionIds.Contains(connectionId))
                    {
                        existingUser.ConnectionIds.Add(connectionId);

         
                        if (existingUser.ConnectionIds.Count == 1)
                        {
                            isNewlyOnline = true;
                        }
                    }
                    return existingUser;
                });

   
            await Clients.Caller.OnlineUserList(_onlineUsers.Values);

 
            if (isNewlyOnline)
            {
                await Clients.Others.UserConnected(user);
                await Clients.Others.UserIsOnline(user.id);
            }

            //await Clients.Caller.OnlineUserList(_onlineUsers.Values);
            //if (!_onlineUsers.ContainsKey(user.id))
            //{  
            //    user.ConnectionId=Context.ConnectionId;
            //    _onlineUsers.TryAdd(user.id, user);
            //    await Clients.Others.UserConnected(user);
            //    await Clients.Others.UserIsOnline(user.id);
            //}

        }
        public async Task MessageSeen(MessageSeenDto messagesForSeen)
        {
            var conversation = await _uow.Conversation
                .Include(x => x.Users)
                .FirstOrDefaultAsync(x => x.Id == messagesForSeen.ConversationId);

            if (conversation is null) return;

            // ۱ — شمارنده‌ی خودم را صفر کن. بی‌قید و شرط.
            var me = conversation.Users.FirstOrDefault(x => x.UserId == messagesForSeen.MyId);
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

            _onlineUsers.Remove(user.id, out var userd);
          var connectedUser=  await usermanager.GetUserBy(user.id);
            connectedUser!.UpdatelastLoginDate();
            await _uow.SaveChangesAsync();
            await Clients.Others.SetLastSeenUser(new LastSeenModelDto(user.id, connectedUser.LastLoginDate!.Value.DateTime));
            await Clients.Others.UserIsOffline(user.id);
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
            if (Lastmessage != null) {
                if (Lastmessage.Id == Guid.Empty) {
                var lastSeenedMessageTimeStamp=await _uow.Messages.AsNoTracking()
                           .Include(x => x.Conversation.Users.Where(x => x.UserId == Lastmessage.UserId))
                              .Where(x => x.Conversation.Users.Any(x => x.UserId == Lastmessage.UserId)
                              &&x.CreatedByUserId!=Lastmessage.UserId&&x.Seen)
                              .OrderByDescending(x=>x.CreateDate).Select(x=>x.CreateDate).FirstOrDefaultAsync();

                    var missedMessage = await _uow.Messages
                .Include(x => x.ChatFiles)
                .Include(x => x.Conversation.Users.Where(x => x.UserId == Lastmessage.UserId))
                    .Where(x => x.Conversation.Users.Any(x => x.UserId == Lastmessage.UserId)
                    && x.CreateDate > lastSeenedMessageTimeStamp
                    )
                    .Select(x => new ChatMessageDto
                    {
                        Id = x.Id,
                        UserId = x.CreatedByUserId.Value,
                        Content = x.Content,
                        IsMine = false,
                        IsSeen = x.Seen,
                        SendAt = x.CreateDate,
                        ParentId = x.ParentMessageId,
                        SenderName = "",
                        Type = (int)x.MessageType,
                        FileContent = x.ChatFiles.Select(cf => new ChatFilesDto
                        {
                            FileId = cf.Id,
                            FileName = cf.FileName,
                            FileSize = cf.FileSize.ToString()
                        }).FirstOrDefault(),

                    }).ToListAsync();
                    if (missedMessage != null)
                    {
                        await Clients.Caller.GetMissedMessages(missedMessage);
                    }
                    return;
                }
            var message = await _uow.Messages
                .Include(x => x.Conversation.Users)
                .Where(x => x.Id == Lastmessage.Id).FirstOrDefaultAsync();
            if (message == null)
                return;
            if (message != null)
            {
                var users = message.Conversation.Users;
                var myuser = users.Where(x => x.UserId != Lastmessage.UserId).FirstOrDefault();
                var otherUser = users.Where(x => x.UserId == Lastmessage.UserId).FirstOrDefault();
                    if (myuser != null)
                    {
                        var missedMessage = await _uow.Messages
                            .Include(x => x.ChatFiles)
                            .Include(x => x.Conversation.Users.Where(x => x.UserId == myuser.UserId))
                                .Where(x => x.Conversation.Users.Any(x => x.UserId == myuser.UserId)
                                && x.CreateDate > Lastmessage.SendAt
                                )
                                .Select(x => new ChatMessageDto
                                {
                                    Id = x.Id,
                                    UserId = x.CreatedByUserId.Value,
                                    Content = x.Content,
                                    IsMine = false,
                                    IsSeen = x.Seen,
                                    SendAt = x.CreateDate,
                                    ParentId = x.ParentMessageId,
                                    SenderName = "",
                                    Type = (int)x.MessageType,
                                    FileContent = x.ChatFiles.Select(cf => new ChatFilesDto
                                    {
                                        FileId = cf.Id,
                                        FileName = cf.FileName,
                                        FileSize = cf.FileSize.ToString()
                                    }).FirstOrDefault(),

                                }).ToListAsync();
                        if (missedMessage != null)
                        {
                            await Clients.Caller.GetMissedMessages(missedMessage);
                        }
                    }
                }
            }



        }
    }
}
