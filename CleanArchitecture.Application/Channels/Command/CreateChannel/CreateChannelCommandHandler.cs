using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.CreateChannel
{
    public sealed class CreateChannelCommandHandler(IApplicationUnitOfWork unitOfWork,
        IApplicationUserManager userManager) : ICommandHandler<CreateChannelCommand>
    {
        private readonly IApplicationUnitOfWork _uow = unitOfWork;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            try
            {
                var userId = _userManager.UserId!.Value;
                var claims = await _userManager.GetUserClaims(userId);

                if (request.IsPublic && !claims.Contains("CreatePublicChannel"))
                    return op.Failed("شما دسترسی ایجاد کانال عمومی رو ندارید");

                if (!request.IsPublic && !claims.Contains("CreatePrivateChannel"))
                    return op.Failed("شما دسترسی ایجاد کانال خصوصی رو ندارید");
                var exists = await _uow.Channels.AnyAsync(c => c.UserName == request.UserName, cancellationToken);
                if (exists) return op.Failed("نام کاربری کانال تکراری است.");

                var channel = request.IsPublic
                    ? Channel.CreatePublicChannel(request.Title, request.UserName, request.Description, userId)
                    : Channel.CreatePrivateChannel(request.Title, request.UserName, request.Description, userId);

                _uow.Channels.Add(channel);

                if (request.IsPublic)
                {
                    var allUserIds = await _uow.Users.Where(x=>x.Id!=userId).Select(x => x.Id).ToListAsync(cancellationToken);
                    var members = ChannelMember.JoinAllMembers(channel.Id, allUserIds);
                    _uow.ChannelMembers.AddRange(members);

                    // اطلاع‌رسانی به کلاینت‌ها (SignalR)
                  //  await _hubContext.Clients.All.SendAsync("ChannelCreated", channel.ToDto(), cancellationToken);
                }

              var result=  await _uow.SaveChangesAsync(cancellationToken);

        

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
