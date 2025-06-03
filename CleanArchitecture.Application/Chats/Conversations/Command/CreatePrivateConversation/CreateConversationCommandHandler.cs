using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Chats.Conversations.Query;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;


namespace CleanArchitecture.Application.Chats.Conversations.Command.CreatePrivateConversation
{
    internal sealed class CreateConversationCommandHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork uow,IMapper mapper,IHttpContextAccessor httpContext) : ICommandHandler<CreateConversationCommand, ConverSationResponse>
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
                var ExistedConverrsation = await _uow.Conversation.AsNoTracking()
            .Include(x => x.Users)
            .ThenInclude(x => x.User.UserAvatars)
            .Where(x =>
                x.Users.Count == 2 && 
                x.Users.All(u => OurChatIds.Contains(u.UserId))
            )
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
          var res=  await _uow.SaveChangesAsync(cancellationToken);
            if (res.IsSuccess)
            {
                var result = _mapper.Map<ConverSationResponse>(privateConversation);
                result.Title = string.Concat(other.LastName, " ", other.FirsName);
            
                return result;
            }
            return OperationResult.Failure<ConverSationResponse>(new OperationResult().Failed("عملیات با خطا مواجه شد!"));
            }
            catch (Exception)
            {

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
