using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Command.PromoteOrDemoteToAdmin
{
    internal sealed class ChangeUserChannelPolicyCommandHandler(IApplicationUnitOfWork uow,
        IApplicationUserManager usermanager) : ICommandHandler<ChangeUserGroupPolicyCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _usermanager = usermanager;

        public async Task<OperationResult> Handle(ChangeUserGroupPolicyCommand request, CancellationToken cancellationToken)
        {
            var userId = _usermanager.UserId!.Value;
            var op = new OperationResult();
            var group = await _uow.Conversation.Include(x => x.Users).FirstOrDefaultAsync(x => x.Id == request.conversationId, cancellationToken);
            if (group == null)
            {
                return op.Failed("group not found");
            }
            var user = group.Users.FirstOrDefault(x => x.UserId == request.UserId);
            if (user == null)
            {
                return op.Failed("user not found");
            }
            if (request.ispromote)
            {
                user.promoteToAdmin(request.UserId);

            }
            else
            {
                user.DemoteFromAdmin(request.UserId);
            }
            await _uow.SaveChangesAsync(cancellationToken);
            return op.succedded();
        }

    }
}
