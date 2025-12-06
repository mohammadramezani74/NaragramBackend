using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.JoinPublicChannel
{
    internal class JoinPublicChannelCommandHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager) : ICommandHandler<JoinPublicChannelCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(JoinPublicChannelCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
            var userId = _userManager.UserId!.Value;
            var isExistMember = await _uow.ChannelMembers.AsNoTracking()
                .AnyAsync(x => x.ChannelId == request.ChannelId && x.UserId == userId);
            if (isExistMember)
            {
                op.Failed("شما قبلا عضو این چنل شده اید!");
            }
  
            var newmember= ChannelMember.Join(request.ChannelId, userId);
            _uow.ChannelMembers.Add(newmember);
            await _uow.SaveChangesAsync();
            return op.succedded();
        }
    }
}
