using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Command.AddNewMemberToGroup
{
    public class AddNewMemberToGroupCommandHandler(IApplicationUnitOfWork uow,IApplicationUserManager userManager) : ICommandHandler<AddNewMemberToGroupCommand>
    {
        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;

        public async Task<OperationResult> Handle(AddNewMemberToGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _uow.Conversation.Include(x => x.Users)
                .Where(x => x.Id == request.ConversationId)
                .FirstOrDefaultAsync();
            if (group == null)
                return new OperationResult().Failed("گروهی یافت نشد!");
            var isExist=group.Users.Any(x=>x.UserId== request.NewUserId);
            if (isExist)
                return new OperationResult().Failed("این کاربر از قبل در این گروه قرار دارد!");

            var newUser = new Domain.Entities.Chat.ConversationUser
            {
                Id = Guid.NewGuid(),
                CreateDate = DateTime.Now,
                Deleted = false,
                ConversationId = group.Id,
                CreatedByUserId = _userManager.UserId.Value,
                Role = Domain.Enums.ConversationRole.Member,
                UserId = request.NewUserId,
            };
            try
            {

          
            _uow.ConversationUser.Add(newUser);
            await _uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                throw;
            }
            return new OperationResult().succedded();
        }
    }
}
