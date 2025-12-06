using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.DeleteChannelMember
{
    internal sealed class DeleteMemberCommandHandler(IApplicationUnitOfWork uow, IHubContext<NaraHub, IChatHubClient> hubContext) : ICommandHandler<DeleteMemberCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult> Handle(DeleteMemberCommand request, CancellationToken cancellationToken)
        {
            var op = new OperationResult();
           var member=await _uow.ChannelMembers.Where(x=>x.UserId==request.memberid&&x.ChannelId==request.channelid).FirstOrDefaultAsync();
            if (member == null) { return op.Failed("عضو مورد نظر یافت نشد"); }
            _uow.ChannelMembers.Remove(member);
          await  _hubContext.Clients.User(request.memberid.ToString()).GetDeletedChannel(request.channelid);
            await _uow.SaveChangesAsync();
            return op.succedded();

        }
    }
}
