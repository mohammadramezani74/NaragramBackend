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
                 .Where(cu => cu.Members.Any(x => x.UserId ==Guid.Parse(userId)))
                 .Select(cu => cu.Id)
                 .ToListAsync();

                foreach (var channelId in channelIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());
                }
            }


#else
            try
            {


                var token = Context.GetHttpContext().Request.Headers.FirstOrDefault(x =>
      x.Key == "Authorization");
                _loger.LogWarning($"token is {token.Value}");
                var validtoken=token.Value.ToString().Replace("Bearer" ,"").Trim();
                _loger.LogWarning($"validtoken is {validtoken}");
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(validtoken);
              var userId=  jwtToken.Claims.Where(x => x.Type == "sub").First().Value;
                if (userId != null) {
                    var channelIds = await _uow.Channels
                 .Where(cu => cu.Members.Any(x => x.UserId == Guid.Parse(userId)))
                 .Select(cu => cu.Id)
                 .ToListAsync();

                    foreach (var channelId in channelIds)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());
                    }
                }
                _loger.LogWarning($"UserId Id {userId}");
                await SetUserOnline(new UserDto(Guid.Parse(userId),string.Empty));

            }
            catch (Exception ex)
            {
                _loger.LogError($"Context.GetHttpContext() is not found");
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
            if (_onlineUsers.ContainsKey(messagesForSeen.UserId))
            {
                var exists = await _uow.Messages
                    .AnyAsync(x => messagesForSeen.messageId.Contains(x.Id));
                if (exists)
                {
                await Clients.User(messagesForSeen.UserId.ToString()).MessagedSeenReceived(messagesForSeen.messageId);
                var conversation=await _uow.Conversation.Include(x=>x.Users.Where(x=>x.ConversationId==messagesForSeen.ConversationId))
                        .FirstOrDefaultAsync(x=>x.Id==messagesForSeen.ConversationId);
                    if(conversation != null)
                    {
                        var user= conversation.Users.Where(x=>x.UserId==messagesForSeen.MyId).FirstOrDefault();
                        user.EmptyCount();
                        await _uow.SaveChangesAsync();
                    }
                }
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
