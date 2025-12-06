using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Users.Queries.GetUser;
using System.Threading.Channels;

namespace CleanArchitecture.Application.Channels.Command.Members.AddMember
{
    public class AddNewMemberCommandHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager, IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<AddNewMemberCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(AddNewMemberCommand request, CancellationToken cancellationToken)
        {
            var op= new OperationResult();
            try
            {
                var userId=_userManager.UserId!.Value;
        
         bool   isExistCurrenUser= await _uow.ChannelMembers.AsNoTracking().AnyAsync(x=>x.ChannelId==request.ChannelId&&x.UserId==request.MemberId);
            if (isExistCurrenUser) {
                op.Failed("این کاربر از قبل در این کانال وجود دارد!");
            }
            var newmember = ChannelMember.Build(request.ChannelId, request.MemberId,userId);
            _uow.ChannelMembers.Add(newmember);
            await _uow.SaveChangesAsync();
                var channel = await _uow.Channels.Where(x => x.Id == request.ChannelId).FirstOrDefaultAsync();
                if (channel != null)
                {
                    var newchannel = new GetUserResponse
                    {
                        IsChannel = true,
                        Id = channel.Id,
                        FirstName = channel.Title,
                        UserName = channel.UserName,

                    };
                    await _hubContext.Clients.User(newmember.UserId.ToString()).ReceiveNewChannel(newchannel);
                }

                return op.succedded();
            }
            catch (Exception ex)
            {

               return op.Failed("خطای غیر منتظره ای رخ داده است لطفا بعدا تلاش فرمایید!");
            }

        }
    }
}
