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

namespace CleanArchitecture.Application.Groups.Command.MuteUser
{
    internal sealed class MuteUserCommandHandler(IApplicationUnitOfWork uow,
        IApplicationUserManager userManager) : ICommandHandler<MuteUserCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(MuteUserCommand request, CancellationToken cancellationToken)
        {
         var op=new OperationResult();
            var muteBy = _userManager.UserId.Value;
            var selectedUser=await _uow.ConversationUser
                .Where(u=>u.ConversationId==request.GroupId&&u.UserId==request.UserId)
                .FirstOrDefaultAsync(cancellationToken);
            if (selectedUser is null)
                return op.Failed("کاربر مورد نظر یافت نشد!");
            selectedUser.Mute(request.isMute, muteBy);
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();
        }
    }
}
