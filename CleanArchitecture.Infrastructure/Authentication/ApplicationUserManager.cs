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
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.ValueObjects;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

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

        if (int.Parse(verifyCode) !=Savedcode)
        {
            return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("کد اعتبار سنجی معتبر نمیباشد"));

        }
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
         var generatedToken= await  _tokenProvider.Generate(existedUser);
            return new TokenResponse(generatedToken.accessToken, generatedToken.refreshToken);
        }
        return OperationResult.Failure<TokenResponse>(new OperationResult().Failed("عملیات با خطا مواجه شد"));

    }

    public async Task<List<GetUserResponse>> GetUsers(GetUserQuery getUser, CancellationToken cancellationToken = default)
    {
        var userList=new List<GetUserResponse>();
        var userId = _httpContextAccessor
            .HttpContext?
            .User
            .GetUserId();

        var conversationsOfUser=await _unitOfWork.Conversation.AsNoTracking()
            .Include(x=>x.Users).ThenInclude(x=>x.User.UserAvatars)
            .Where(x=>x.Users.Any(x=>x.UserId== userId))
            .ToListAsync();
        var unreadMessages = await _unitOfWork.Messages.AsNoTracking()
            .Include(x=>x.Conversation.Users)
            .Where(x => x.Conversation.Users.Any(x => x.UserId == userId) &&
            x.Seen == false).ToListAsync(cancellationToken);
        var AllUsers=await _unitOfWork.Messages.AsNoTracking()
            .Where(x=>x.Conversation.Users.Any(x=>x.UserId== userId))
            .Select(x=>x.ConversationId).Distinct().ToListAsync();

        var conversationsorted= conversationsOfUser.OrderByDescending(x=> AllUsers.Contains(x.Id)).ToList();

        foreach (var conversation in conversationsorted)
        {
            var users = conversation.Users.ToList();
            foreach (var usertarget in users)
            {if(usertarget.UserId!= userId) {
                var model = new GetUserResponse
                {
                    Id = usertarget.User.Id,
                    ConversationId= conversation.Id,
                    MessageUnreadedCount= unreadMessages.Where(x=>x.CreatedByUserId== usertarget.User.Id).Count(),
                    LastSeen = usertarget.User.LastLoginDate.HasValue? usertarget.User.LastLoginDate.Value.DateTime:null,
                    Address = usertarget.User.Address,
                    Age = usertarget.User.Age.ToString(),
                    FirstName = usertarget.User.FirsName!,
                    LastName = usertarget.User.LastName!,
                    UserName = usertarget.User.UserName!,
                    Avatar = SetAvatar(usertarget.User)


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
                UserName = x.UserName!,
                Avatar = SetAvatar(x)
            })
            .ToListAsync();


        return userList.Union(userswithoutConversation).ToList();
    }
    public async Task<OperationResult>SendValidateCode(string phoneNumber)
    {
        var op = new OperationResult();
        var number = Random.Shared.Next(10000, 99999);
       User? user= await _userManager.Users.Where(x => x.PhoneNumber.Equals(phoneNumber)).FirstOrDefaultAsync();
        if(user is null)
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
            await _smsService.SendVerificationCode(phoneNumber, number.ToString());
            return op.succedded();
        }
        //user.updateNationalCode(number);
        //await _unitOfWork.SaveChangesAsync();
        _cacheService.Set(phoneNumber, number, TimeSpan.FromMinutes(2));
        await _smsService.SendVerificationCode(phoneNumber, number.ToString());
        return op.succedded();

    }
    public static string? SetAvatar(User x)
    {
        
      var thumbnail=  x.UserAvatars?.FirstOrDefault()?.FileData??null;
        if (thumbnail != null) {
           return Convert.ToBase64String(thumbnail);
        }
        return null;
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
