using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Command.CreateGroupConverSation
{
    internal sealed class CreateGroupConversationCommandHandler(IApplicationUserManager userManager,
        IApplicationUnitOfWork uow,
        CleanArchitecture.Application.AdminPanel.ICreationPolicy creationPolicy,
        CleanArchitecture.Application.Hubs.HubRoomMembership rooms) : ICommandHandler<CreateGroupConversationCommand>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(CreateGroupConversationCommand request, CancellationToken cancellationToken)
        {
            if (!await creationPolicy.CanCreateAsync(_userManager.UserId!.Value, "group", cancellationToken))
                return new OperationResult().Failed("ساخت گروه برای شما فعال نیست. با مدیر سیستم هماهنگ کنید.");

            var AllExists = request.Others.All( u =>  _userManager.ExistUserBy(u));
            var me = await _userManager.GetUserBy(_userManager.UserId!.Value);
     
            var others=await _uow.Users.Where(x=>request.Others.Contains(x.Id) && !x.IsBot).ToListAsync(); // ربات عضو گروه نمی‌شود
            var group = Domain.Entities.Chat.Conversation.Create(me!, others,request.Title,request.Description,request.UserName);
        
            _uow.Conversation.Add(group);
           
            var saved = await _uow.SaveChangesAsync();
            if (saved.IsFailure)
                return new OperationResult().Failed("ساخت گروه با خطا مواجه شد.");
            // سازنده و اعضا بدون وصل شدن دوباره پیام‌های زنده‌ی گروه را بگیرند.
            await rooms.AddAsync(group.Id, group.Users.Select(u => u.UserId));
            return new  OperationResult().succedded();
        }
    }
}
