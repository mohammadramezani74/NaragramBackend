using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Caching;
using CleanArchitecture.Application.Abstraction.Sms;
using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Common.Utilities.Extensions.DateExtensions;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using CleanArchitecture.Application.Users.Queries.GetUser;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Authentication;

internal class ApplicationUserManager(IHttpContextAccessor httpContextAccessor,
    UserManager<User> userManager,
    IApplicationUnitOfWork unitOfWork,
    IApplicationRoleManager roleManager,
    ITokenProvider tokenProvider,
    ICacheService cacheService,
    ISmsService smsService,
    IHubContext<NaraHub, IChatHubClient> hub) : IApplicationUserManager
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly ICacheService _cacheService = cacheService;
    private readonly ISmsService _smsService = smsService;
    IHubContext<NaraHub, IChatHubClient> _hub=hub;
    private readonly UserManager<User> _userManager = userManager;
    private readonly IApplicationRoleManager _roleManager= roleManager;
    private readonly IApplicationUnitOfWork _unitOfWork = unitOfWork;
    public Guid? UserId =>
        _httpContextAccessor
            .HttpContext?
            .User
            .GetUserId() ??null
   /* throw new ApplicationException("User context is unavailable")*/;

    public async Task<OperationResult> CreateUserAsync(CreateUserCommand createUser, CancellationToken cancellationToken = default)
    {
        var address = createUser.Address;
        var user = User.Create(createUser.UserName, createUser.Age, createUser.Email, createUser.LastName, createUser.FirstName
               , createUser.Gender, createUser.phoneNumber, address
               );

        var result = await _userManager.CreateAsync(user, createUser.Password);
        if (result.Succeeded)
        {
            await hub.Clients.All.UserConnected(new UserDto(user.Id, user.LastName+ " "+user.FirsName));
            return new OperationResult().succedded();
        }
        foreach (var error in result.Errors)
        {
            throw new BadRequestException(error.Description);
        }
        return new OperationResult().Failed("عملیات با خطا مواجه شد");
    }
    public async Task<OperationResult<TokenResponse>> CreateOrLoginUserAsync(string phoneNumber,string verifyCode, CancellationToken cancellationToken = default)
    {
       var Savedcode= _cacheService.Get<int>(phoneNumber);

        //if (int.Parse(verifyCode) !=Savedcode)
        //{
        //    return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("کد اعتبار سنجی معتبر نمیباشد"));

        //}
        var existedUser = await _userManager.Users.Where(x => x.PhoneNumber == phoneNumber.Trim()).FirstOrDefaultAsync();
        if (existedUser == null)
        {
            try
            {

         
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.irannara.com/api/v1/Telegram/AuthForChat");
            request.Headers.Add("accept", "*/*");
            var content = new StringContent($"\"{phoneNumber}\"", null, "application/json-patch+json");
            request.Content = content;
            var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var target = JsonSerializer.Deserialize<IntsAuthResult>(await response.Content.ReadAsStringAsync(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var user = User.CreateWithPhoneNumber(target!.result.id,phoneNumber, target!.result.lastName, target!.result.firstName,
                        new Address(target!.result.city,null,null),
                        target.result.chartPost
                 );
                    var Createdresponse = await _userManager.CreateAsync(user, "Nara@@123914");
                    if (Createdresponse.Succeeded)
                    {
                        await hub.Clients.All.UserConnected(new UserDto(user.Id, user.LastName + " " + user.FirsName));
                       var generatedToken = await _tokenProvider.Generate(user);
                        return new TokenResponse(generatedToken.accessToken, generatedToken.refreshToken);
                    }
                }
                else
                {
              
                    return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("کاربری با این شماره همراه یافت نشد لطفا با پشتیبانی تماس بگیرید"));
                }
            }
            catch (Exception)
            {

                return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("عملیات با خطا مواجه شد"));
            }
          
           
        }
        else
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.irannara.com/api/v1/Telegram/AuthForChat");
            request.Headers.Add("accept", "*/*");
            var content = new StringContent($"\"{phoneNumber}\"", null, "application/json-patch+json");
            request.Content = content;
            var response = await client.SendAsync(request);
            var target = JsonSerializer.Deserialize<IntsAuthResult>(await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (target.result.NationalCode == verifyCode)
            {
                var generatedToken = await _tokenProvider.Generate(existedUser);
                return new TokenResponse(generatedToken.accessToken, generatedToken.refreshToken);
            }
            else
            {
                return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("کاربری با این شماره همراه یافت نشد لطفا با پشتیبانی تماس بگیرید"));
            }
       
        }
        return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("عملیات با خطا مواجه شد"));

    }

    public async Task<List<GetUserResponse>> GetUsers2(GetUserQuery getUser, CancellationToken cancellationToken = default)
    {
        var userList=new List<GetUserResponse>();
        var userId = _httpContextAccessor
            .HttpContext?
            .User
            .GetUserId();

        var conversationsOfUser=await _unitOfWork.Conversation.AsNoTracking()
            .Include(x=>x.Users.Where(x=>x.UserId!=userId))
            .ThenInclude(x=>x.User.UserAvatars)
            .Where(x=>x.Users.Any(x=>x.UserId== userId))
            .ToListAsync();



        var conversationIds = conversationsOfUser.Select(c => c.Id).ToList();

        var unreadMessages = await _unitOfWork.Messages.AsNoTracking()
            .Where(x => conversationIds.Contains(x.ConversationId.Value) && !x.Seen
            &&x.CreatedByUserId!=userId)
            .ToListAsync(cancellationToken);

        var lastMessages = _unitOfWork.Messages
           .Where(m => conversationIds.Contains(m.ConversationId.Value))
           .GroupBy(m => m.ConversationId)
           .Select(g => g
               .OrderByDescending(m => m.CreateDate)
               .FirstOrDefault())
           .ToList();

        var AllUsers=await _unitOfWork.Messages.AsNoTracking()
            .Where(x=>x.Conversation.Users.Any(x=>x.UserId== userId))
            .Select(x=>x.ConversationId).Distinct().ToListAsync();

        var conversationsorted= conversationsOfUser.OrderByDescending(x=> AllUsers.Contains(x.Id)).ToList();

        foreach (var conversation in conversationsorted)
        {
            var users = conversation.Users.ToList();
            foreach (var usertarget in users)
            {
                string messagetitle = string.Empty;
                Guid? MessageId = null;
                bool lastmessageForme = false;
                DateTime MessageSendDate=DateTime.Now;
                if (usertarget.UserId!= userId) {

                    var lastmessage = lastMessages.Where(x => x.ConversationId == conversation.Id)
                        .FirstOrDefault();
                    if (lastmessage != null)
                    {
                        messagetitle = lastmessage.MessageType == Domain.Enums.MessageType.Text
     ? (lastmessage.Content.Length > 30 ? lastmessage.Content.Substring(0, 30) + "..." : lastmessage.Content)
     : (GetMessageFormat(lastmessage.MessageType).Length > 30 ? "..." + GetMessageFormat(lastmessage.MessageType).Substring(0, 30) : GetMessageFormat(lastmessage.MessageType));
                        MessageId = lastmessage.Id;
                        MessageSendDate = lastmessage.CreateDate;
                        lastmessageForme = lastmessage.CreatedByUserId == userId;
                    }
                    var model = new GetUserResponse
                        {
                            Id = usertarget.User.Id,
                            ConversationId = conversation.Id,
                            MessageUnreadedCount = unreadMessages.Where(x => x.CreatedByUserId == usertarget.User.Id).Count(),
                            LastSeen = usertarget.User.LastLoginDate.HasValue ? usertarget.User.LastLoginDate.Value.DateTime : null,
                            Address = usertarget.User.Address,
                            Age = usertarget.User.Age.ToString(),
                            FirstName = usertarget.User.FirsName!,
                            LastName = usertarget.User.LastName!,
                            UserName = usertarget.User.UserName!,
                            Avatar = SetAvatar(usertarget.User),
                            LastReceivedMessage=messagetitle,
                            LastReceivedMessageId=MessageId,
                            IsLastReceivedMessageForMe= lastmessageForme,
                            LastReceivedMessageSendDate=MessageSendDate.FormatPersianDate().ToPersianNumber()

                        };
                userList.Add(model); 
                }
            }
         

        }
        var userswithoutConversation=await _unitOfWork.Users.Include(x=>x.UserAvatars)
            .AsNoTracking().Where(u=>!userList.Select(x=>x.Id).ToList().Contains(u.Id)
            &&u.Id!=userId)
            .Select(x => new GetUserResponse
            {
                Id = x.Id,
                Address = x.Address,
                Age = x.Age.ToString(),
                FirstName = x.FirsName!,
                LastName = x.LastName!,
                //UserName = x.UserName!,
                //Avatar = SetAvatar(x)
            })
            .ToListAsync();
      

        return userList.Union(userswithoutConversation).ToList();
    }
    public async Task<List<GetUserResponse>> GetUsers(GetUserQuery getUser, CancellationToken cancellationToken = default)
    {
        try
        {

    
        var userId = _httpContextAccessor
            .HttpContext?
            .User
            .GetUserId();

            var response = new List<GetUserResponse>();

            var conversationsOfUser = await _unitOfWork.Conversation.Where(x=>x.IsPrivate).AsNoTracking()
                         .AsSplitQuery()
                .Include(x => x.Users)
                .ThenInclude(x => x.User)
                //.ThenInclude(x => x.UserAvatars.Where(x => x.UserId != userId))
                .Where(x => x.Users.Any(x => x.UserId == userId))

                .ToListAsync();

            foreach (var conversation in conversationsOfUser)
            {
                var OtherUser = conversation.Users.Where(u => u.UserId != userId).Select(u => u.User).FirstOrDefault();
                var CurrentUser = conversation.Users.Where(u => u.UserId == userId).FirstOrDefault();
                var OtherconversationUser = conversation.Users.Where(u => u.UserId != userId).FirstOrDefault();
                if (OtherUser != null)
                {
                    var newuser = new GetUserResponse

                    {
                        Id = OtherUser.Id,
                        ConversationId = conversation.Id,
                        MessageUnreadedCount = CurrentUser?.UnreadCount ?? 0,
                        LastSeen = OtherUser.LastLoginDate?.DateTime,
                        FirstName = OtherUser.FirsName!,
                        LastName = OtherUser.LastName!,
                        UserName = OtherUser.UserName!,
                        Avatar = SetAvatar(OtherUser),
                        LastReceivedMessage = conversation.LastMessageText,
                        LastReceivedMessageId = conversation.LastMessageId,
                        IsLastReceivedMessageForMe = conversation.LastUserSenderMessageId == userId,
                        LastReceivedMessageSendDate = conversation.LastMessageSentAt?.FormatPersianDate().ToPersianNumber() ?? string.Empty,
                        LastMessageDate = conversation.LastMessageSentAt,
                        IsPin = CurrentUser.IsPinned,
                        IsBlocked = CurrentUser.IsBlocked,
                        OtherUserBlocked = OtherconversationUser.IsBlocked
                    };
                    response.Add(newuser);
                }
            }

            var userIdsWithConversation = response.Select(x => x.Id).ToList();

        var userswithoutConversation = await _unitOfWork.Users
                      .AsNoTracking()
                      .AsSplitQuery()
                //.Include(x => x.UserAvatars)
            .Where(u => !userIdsWithConversation.Contains(u.Id)
            && u.Id != userId)
            .Select(x => new GetUserResponse
            {
                Id = x.Id,
                Address = x.Address,
                Age = x.Age.ToString(),
                FirstName = x.FirsName!,
                LastName = x.LastName!,
                UserName = x.UserName!,

                Avatar = SetAvatar(x,false)
            })
            .ToListAsync();
            var channelsquery = await _unitOfWork.Channels.AsNoTracking()
                 .AsSplitQuery()
                .Include(x => x.Members.Where(x => x.UserId == userId))
                .Include(x => x.CreatedByUser)
                //.Include(x => x.ChannelAvatars)
                .Include(x => x.Admins).ThenInclude(x => x.User)

            .Where(x => x.Members.Any(x => x.UserId == userId))
          .ToListAsync(cancellationToken);
            var channels = channelsquery
        .Select(x => new GetUserResponse
        {
            Id = x.Id,
            FirstName = x.Title,
            UserName = x.UserName,
            Bio=x.Description,
            LastReceivedMessage = x.LastMessageText,
            LastReceivedMessageId = x.LastMessageId,
            IsLastReceivedMessageForMe = false,
            MessageUnreadedCount=CalculateCount(x.Members,userId),
            LastReceivedMessageSendDate = CreateDateTime(x.LastMessageSentAt),
            LastMessageDate=x.LastMessageSentAt,
            IsChannel = true,
            channel = new ChannelDto
            {
                Creator = x.CreatedByUser.LastName + " " + x.CreatedByUser.FirsName,
                CreatorId = x.CreatedByUserId.Value,
                admins = GetAdmins(x.Admins, x.CreatedByUserId),
                CurrentUserAdmin = IsUserAdmin(x, userId.Value)
            },
            Avatar = SetChannelAvatar(x)


        })
        .ToList();

            var rawGroups = await _unitOfWork.Conversation.AsNoTracking()
               .Include(x => x.CreatedByUser)
               .Include(x => x.Users)
                   .ThenInclude(x => x.User)
               .Where(x => x.IsPrivate == false && x.Users.Any(u => u.UserId == userId))
               .AsSplitQuery()
               .ToListAsync();

            var groups = rawGroups.Select(x => new GetUserResponse
            {
                Id = x.Id,
                FirstName = x.Title,
                UserName = x.UserName,
                LastReceivedMessage = x.LastMessageText,
                Bio = x.Description,
                Age = x.Users.Count.ToString(),
                LastReceivedMessageId = x.LastMessageId,
                IsLastReceivedMessageForMe = false,
                MessageUnreadedCount = CalculateGroupsCount(x.Users, userId),
                LastReceivedMessageSendDate = CreateDateTime(x.LastMessageSentAt),
                LastMessageDate = x.LastMessageSentAt,
                IsGroup = true,
                channel = new ChannelDto
                {
                    Creator = x.CreatedByUser.LastName + " " + x.CreatedByUser.FirsName,
                    CreatorId = x.CreatedByUserId.Value,
                    admins = GetGroupAdmins(x.Users, x.CreatedByUserId),
                    CurrentUserAdmin = IsGroupUserAdmin(x, userId.Value)
                },
                Avatar = SetGoupAvatar(x)
            }).ToList();


            return response.Concat(userswithoutConversation)
            .Concat(channels)
            .Concat(groups)
             .OrderByDescending(x => x.IsPin)
            .ThenByDescending(x=>x.LastMessageDate)
            .ThenByDescending(x=>x.Avatar!=null)
            .ThenByDescending(x=>x.ConversationId!=Guid.Empty)
            .ToList();
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private static int CalculateCount(ICollection<ChannelMember> members, Guid? userId)
    {
        var member = members.Where(x => x.UserId == userId).FirstOrDefault();
        if (member == null) return 0;
        return member.UnreadCount;
    }
    private static int CalculateGroupsCount(ICollection<ConversationUser> members, Guid? userId)
    {
        var member = members.Where(x => x.UserId == userId).FirstOrDefault();
        if (member == null) return 0;
        return member.UnreadCount;
    }

    private static bool IsUserAdmin(Channel x,Guid userId)
    {
      if( x.CreatedByUserId== userId)
            return true;
      else if(x.Admins.Any(x=>x.UserId== userId))
        {
            return true;
        }
      return false;
    }

    private static List<UserChannelDto> GetAdmins(IReadOnlyCollection<ChannelAdmin> admins,Guid? CreatorId)
    {
      var list= new List<UserChannelDto>();
        foreach (var admin in admins.Where(x=>x.UserId!=CreatorId).ToList())
        {
            list.Add(new UserChannelDto
            {
                Id = admin.UserId,
                IsAdmin = true,
                Name = admin.User.FirsName + " " + admin.User.LastName
            });
        }
        return list;
    }
    private static bool IsGroupUserAdmin(Conversation x, Guid userId)
    {
        var isEsixt=x.Users.Any(x=>x.UserId == userId&& x. IsAdmin);
        if (x.CreatedByUserId == userId)
            return true;
        else if (isEsixt)
        {
            return true;
        }
        return false;
    }

    private static List<UserChannelDto> GetGroupAdmins(ICollection<ConversationUser> admins, Guid? CreatorId)
    {
        var list = new List<UserChannelDto>();
        foreach (var admin in admins.Where(x => x.UserId != CreatorId &&x.IsAdmin).ToList())
        {
            list.Add(new UserChannelDto
            {
                Id = admin.UserId,
                IsAdmin = true,
                Name = admin.User.FirsName + " " + admin.User.LastName
            });
        }
        return list;
    }

    private static string CreateDateTime(DateTime? lastMessageSentAt)
    {
       return lastMessageSentAt?.FormatPersianDate().ToPersianNumber() ?? "";
    }

    private static string GetMessageFormat(MessageType messageType)
    {
        string Messagetype = messageType switch
        {
            MessageType.Video => "پیام ویدیویی",
            MessageType.Audio =>"پیام صوتی",
            MessageType.Image =>"پیام تصویری",
            MessageType.Document=>"پیام  اسنادی",
              MessageType.Location => "لوکیشن"
        };
        return Messagetype;
    }

    public async Task<OperationResult>SendValidateCode(string phoneNumber)
    {
        var op = new OperationResult();
        var number = Random.Shared.Next(10000, 99999);
       User? user= await _userManager.Users.Where(x => x.PhoneNumber.Equals(phoneNumber)).FirstOrDefaultAsync();

        var message = $"کاربر گرامی در تاریخ {DateTime.Now.ToFarsiFull()} به اکانت ناراگرام شما با آیپی {httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress.ToString()} لاگین شد.";
        if (user is null)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.irannara.com/api/v1/Telegram/AuthForChat");
            request.Headers.Add("accept", "*/*");
            var content = new StringContent($"\"{phoneNumber}\"", null, "application/json-patch+json");
            request.Content = content;
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
             return   op.Failed("کاربری با این شماره همراه یافت نشد");
            _cacheService.Set(phoneNumber, number, TimeSpan.FromMinutes(2));
          //  await _smsService.SendMessageToUser(phoneNumber, message);
            return op.succedded();
        }
        //user.updateNationalCode(number);
        //await _unitOfWork.SaveChangesAsync();
        _cacheService.Set(phoneNumber, number, TimeSpan.FromMinutes(2));
       await _smsService.SendMessageToUser(phoneNumber, message);
        return op.succedded();

    }
    public static string? SetAvatar(User x,bool ischannel=false)
    {
        
    
           return $"api/v1/chatfiles/{x.Id}/{ischannel}/getAvatar";
        
        return null;
    }
    public static string? SetChannelAvatar(Channel x)
    {

   
      
            return $"api/v1/chatfiles/{x.Id}/{true}/getAvatar";
        
    }
 
    public static string? SetGoupAvatar(Conversation x)
    {



        return $"api/v1/chatfiles/{x.Id}/getgroupAvatar";

    }



    public async Task<List<string>> GetUserClaims(Guid userId, CancellationToken cancellationToken = default)
    {
    
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new List<string> { "کاربر مورد نظر یافت نشد" };
        }

      
        var claims = await _userManager.GetClaimsAsync(user);
        if (claims == null || !claims.Any())
        {
            return new List<string> { "هیچ ادعایی برای این کاربر وجود ندارد" };
        }

    
        var claimList = claims.Select(c =>c.Value).ToList();

        return claimList;
    }



    public async Task<OperationResult> CreateUserClaimsAsync(Guid UserId, List<string> claims, CancellationToken cancellationToken = default(CancellationToken))
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == UserId, cancellationToken);
        if (user is null) return new OperationResult().NotFound("کاربر مورد نظر یافت نشد");
        var claimValues = claims
                               .Select(c => c.Trim())
                               .ToList();


        var existingClaims = await _userManager.GetClaimsAsync(user);


        var validNewClaims = claimValues
            .Where(claimValue =>
                !existingClaims.Any(existingClaim =>
                    existingClaim.Type == "Permission" && existingClaim.Value == claimValue))
            .Select(claimValue => new Claim("Permission", claimValue))
            .ToList();


        if (!validNewClaims.Any())
        {
            return new OperationResult().succedded("کلایم‌های جدیدی برای اضافه کردن وجود ندارد.");
        }


        var result = await _userManager.AddClaimsAsync(user, validNewClaims);


        if (result.Succeeded)
        {
            return new OperationResult().succedded("کلایم‌ها با موفقیت اضافه شدند.");
        }
        else
        {

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new OperationResult().Failed(errors);
        }
    }

    public async Task<User?> GetUserBy(string username, string password)
    {
       
        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            return null; 
        }

    
        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return null;
        }

        return user; 
    }
    public async Task<bool> ChangePasswordAsync(string nationalCode, string phoneNumber, string password)
    {
        var user = _userManager.Users.SingleOrDefault(u => u.NationalCode == nationalCode && u.PhoneNumber == phoneNumber);

        if (user == null)
        {
            return false;
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return false;
        }

        return true;
    }

    public async Task<User?> GetUserBy(Guid Id)
    {
       var user= await _userManager.FindByIdAsync(Id.ToString());
        return user;
    }
    public async Task<List<string>> GetAllCalimsByUserId(Guid UserId)
    {
        var user = await _userManager.FindByIdAsync(UserId.ToString());
        var claims = new List<string>();
        var Uclaims = await _userManager.GetClaimsAsync(user!);
        var userClaims = Uclaims.Select(x => x.Value).ToList();
        claims.AddRange(userClaims);
        var roles = await _roleManager.GetRolesByUserId(user!.Id);
        List<Guid> RoleIds = new();
        foreach (var role in roles)
        {

            RoleIds.Add(role.RoleId);
        }
        if (RoleIds.Count > 0)
        {
            foreach (var roleId in RoleIds)
            {
                var roleClaims = await _roleManager.GetClaims(roleId);
                foreach (string claim in roleClaims)
                {
                    claims.Add(claim);
                }
            }

        }

        return claims;

    }

    public bool ExistUserBy(Guid Id)
    {
     return  _userManager.Users.Any(x => x.Id == Id);
    }
   
}
