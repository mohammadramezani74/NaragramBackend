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

namespace CleanArchitecture.Application.Channels.Command.PromoteOrDemotToAdmin
{
    public sealed record ChangeUserChannelPolicyCommand(Guid channelId,Guid UserId,bool ispromote):ICommands;
    internal sealed class ChangeUserChannelPolicyCommandHandler(IApplicationUnitOfWork uow,
        IApplicationUserManager usermanager) : ICommandHandler<ChangeUserChannelPolicyCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _usermanager = usermanager;

        public async Task<OperationResult> Handle(ChangeUserChannelPolicyCommand request, CancellationToken cancellationToken)
        {
            var userId=_usermanager.UserId!.Value;
            var op = new OperationResult();
            var channel = await _uow.Channels.FirstOrDefaultAsync(x => x.Id == request.channelId,cancellationToken);
            if (channel == null)
            {
                return op.Failed("Channel not found");
            }
            if (request.ispromote)
            {
                var newAdmin = ChannelAdmin.Create(request.channelId, request.UserId, true, true, true);
                _uow.ChannelAdmins.Add(newAdmin);
            }
            else
            {
              var admin=  await _uow.ChannelAdmins.FirstOrDefaultAsync(x => x.ChannelId ==
                request.channelId && x.UserId == request.UserId);
                _uow.ChannelAdmins.Remove(admin);
            }
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();
        }
    }
}
