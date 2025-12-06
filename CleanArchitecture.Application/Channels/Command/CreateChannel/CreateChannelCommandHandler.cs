using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CleanArchitecture.Application.Users.Queries.GetUser;

namespace CleanArchitecture.Application.Channels.Command.CreateChannel
{
    public sealed class CreateChannelCommandHandler(IApplicationUnitOfWork unitOfWork,
        IApplicationUserManager userManager,
            IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<CreateChannelCommand>
    {
        private readonly IApplicationUnitOfWork _uow = unitOfWork;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> hubContext = hubContext;

        public async Task<OperationResult> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            try
            {
                var userId = _userManager.UserId!.Value;
                var claims = await _userManager.GetUserClaims(userId);
                List<Guid>? allUserIds = new List<Guid>();

                if (request.IsPublic && !claims.Contains("CreatePublicChannel"))
                    return op.Failed("شما دسترسی ایجاد کانال با تمامی پرسنل را ندارید");

                if (!request.IsPublic && !claims.Contains("CreatePrivateChannel"))
                    return op.Failed("شما دسترسی ایجاد کانال را ندارید");
                var exists = await _uow.Channels.AnyAsync(c => c.UserName == request.UserName, cancellationToken);
                if (exists) return op.Failed("نام کاربری کانال تکراری است.");
                var existsName = await _uow.Channels.AnyAsync(c => c.Title.Trim() == request.Title.Trim(), cancellationToken);
                if (existsName) return op.Failed("نام  کانال تکراری است.");
                Channel channel = Channel.CreatePublicChannel(request.Title, request.UserName, request.Description, userId);
                if (request.IsPublic)
                {
                     allUserIds = await _uow.Users.Where(x => x.Id != userId).Select(x => x.Id).ToListAsync(cancellationToken);
                    var members = ChannelMember.JoinAllMembers(channel.Id, allUserIds);
                    _uow.ChannelMembers.AddRange(members);
                }

                _uow.Channels.Add(channel);

          

              var result=  await _uow.SaveChangesAsync(cancellationToken);
                if (request.IsPublic) {
                    var newchannel = new GetUserResponse
                    {
                        IsChannel = true,
                        Id=channel.Id,
                        FirstName=channel.Title,
                        UserName=channel.UserName,
                    
                    };
                foreach (var memberid in allUserIds)
                {
                        await hubContext.Clients.User(memberid.ToString()).ReceiveNewChannel(newchannel);
                }
 }
                return op.succedded();
            }
            catch (DomainException ex)
            {
                return op.Failed(ex.Message);
            }
            catch (Exception ex)
            {
                return op.Failed("عملیات با خطا مواجه شد!");
            }
        }
    }
}
