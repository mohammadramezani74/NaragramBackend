using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Command.RemoveMemberFromGroup
{
    public class RemoveMemberFromGroupCommandHandler(IApplicationUnitOfWork uow) : ICommandHandler<RemoveMemberFromGroupCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult> Handle(RemoveMemberFromGroupCommand request, CancellationToken cancellationToken)
        {
            try
            {

        
            var group = await _uow.ConversationUser
                .Where(x => x.ConversationId == request.ConversationId
                &&x.UserId==request.memberId)
                .FirstOrDefaultAsync();
            if (group == null)
                return new OperationResult().Failed("گروهی یافت نشد!");

                _uow.ConversationUser.Remove(group);
            await _uow.SaveChangesAsync();
            return new OperationResult().succedded();
            }
            catch (Exception ex)
            {

                throw;
            }

        }
    }
}
