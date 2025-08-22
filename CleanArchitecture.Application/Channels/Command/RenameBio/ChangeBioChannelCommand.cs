using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Command.RenameBio
{
    public sealed record ChangeBioChannelCommand(
        Guid ChannelId,
        string bio
        ):ICommands;

    internal sealed class ChangeBioChannelCommandHandler(IApplicationUnitOfWork uow,IApplicationUserManager usermanager) : ICommandHandler<ChangeBioChannelCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _usermanager = usermanager;

        public async Task<OperationResult> Handle(ChangeBioChannelCommand request, CancellationToken cancellationToken)
        {var userId= _usermanager.UserId.Value;
            var op =new OperationResult();
           var channel=await _uow.Channels.Include(x=>x.Admins).Where(x => x.Id == request.ChannelId)
                .FirstOrDefaultAsync(cancellationToken);
            if (channel == null) {
              return  op.Failed(" کانال مورد نظر یافت نشد!");
            }
            channel.ChangeDescription(request.bio, userId);
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();
           
        }
    }
}
