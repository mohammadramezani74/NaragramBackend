using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.Conversations.Query;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;


namespace CleanArchitecture.Application.Chats.Conversations.Command.CreatePrivateConversation
{
    internal sealed class CreateConversationCommandHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork uow,IMapper mapper,IHttpContextAccessor httpContext,
        ILogger<CreateConversationCommandHandler> logger) : ICommandHandler<CreateConversationCommand, ConverSationResponse>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IMapper _mapper = mapper;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContext;

        public async Task<OperationResult<ConverSationResponse>> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
        {
            try
            {

                if (request is null) { throw new ArgumentNullException(nameof(request)); }
            var OurChatIds=new List<Guid>();
            var me = await _userManager.GetUserBy(_userManager.UserId!.Value);
            OurChatIds.Add(me.Id);
            var other = await _userManager.GetUserBy(request.ToUserId);
            OurChatIds.Add(other.Id);
            // جستجو با جفت مرتب‌شده روی ایندکس یکتا، به‌جای بررسی زیرمجموعه‌ی
            // کاربرانِ هر گفتگو.
            var a = me.Id.CompareTo(other.Id) <= 0 ? me.Id : other.Id;
            var b = me.Id.CompareTo(other.Id) <= 0 ? other.Id : me.Id;

                var ExistedConverrsation = await _uow.Conversation.AsNoTracking()
            .Include(x => x.Users)
            .ThenInclude(x => x.User.UserAvatars)
            .Where(x => x.IsPrivate && x.UserAId == a && x.UserBId == b)
            .FirstOrDefaultAsync(cancellationToken);
                if (ExistedConverrsation != null) 
            {
               var result= _mapper.Map<ConverSationResponse>(ExistedConverrsation);
                result.Title = string.Concat(other.LastName, " ", other.FirsName);
                
                    return result;
                }

                if (me.Id == other.Id) {
                    return new ConverSationResponse();
                }
            var privateConversation = Domain.Entities.Chat.Conversation
                  .Create(me!, other!);
            _uow.Conversation.Add(privateConversation);

            try
            {
              var res=  await _uow.SaveChangesAsync(cancellationToken);
                if (res.IsSuccess)
                {
                    var result = _mapper.Map<ConverSationResponse>(privateConversation);
                    result.Title = string.Concat(other.LastName, " ", other.FirsName);

                    return result;
                }
                logger.LogWarning("CreateConversation save failed ({Me} -> {Other}): {Error}", me.Id, other.Id, res.Message);
            }
            catch (DbUpdateException)
            {
                // درخواست هم‌زمان دیگری زودتر ساخته و ایندکس یکتا جلوی دومی را
                // گرفت. همان گفتگو را برمی‌گردانیم تا کاربر خطا نبیند — این
                // دقیقاً همان حالتی است که تا الان گفتگوی تکراری می‌ساخت.
                _uow.Conversation.Entry(privateConversation).State = EntityState.Detached;

                var winner = await _uow.Conversation.AsNoTracking()
                    .Include(x => x.Users)
                    .ThenInclude(x => x.User.UserAvatars)
                    .Where(x => x.IsPrivate && x.UserAId == a && x.UserBId == b)
                    .FirstOrDefaultAsync(cancellationToken);

                if (winner is not null)
                {
                    var result = _mapper.Map<ConverSationResponse>(winner);
                    result.Title = string.Concat(other.LastName, " ", other.FirsName);

                    return result;
                }
            }
            return OperationResult.Failure<ConverSationResponse>(new OperationResult().Failed("عملیات با خطا مواجه شد!"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreateConversation failed for {ToUserId}", request?.ToUserId);
                return OperationResult.Failure<ConverSationResponse>(new OperationResult().Failed("عملیات با خطا مواجه شد!"));
            }
        }
        public static string SetAvatar(User x, string hostName, string scheme)
        {
            string RootAddress = $"{scheme}://{hostName}";
            var avatar = x.Avatar;
            if (avatar != null)
            {
                return RootAddress + avatar;
            }
            else
            {
                if (x.Gender == Domain.Enums.Gender.Male)
                {
                    return RootAddress + "/ChatFiles/Defaults/male.png";
                }
                else
                {

                    return RootAddress + "/ChatFiles/Defaults/female.png";

                }
            }
        }
    }
}
