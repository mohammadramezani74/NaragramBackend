using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Groups.Query.GroupMembers
{
    public sealed class GetGroupMembersQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<GetGroupMembersQuery, IReadOnlyList<GroupMemberViewModel>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<IReadOnlyList<GroupMemberViewModel>>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
        {
            var result = await _uow.Conversation.AsNoTracking()
                 .Include(x => x.Users)
                 .ThenInclude(x=>x.User)
                 .Where(x => x.Id == request.ConversationId)
                 .FirstOrDefaultAsync();
            if(result == null)  return new List<GroupMemberViewModel>();  
            var users = result.Users.Select(x=>new GroupMemberViewModel
            {
                Id = x.UserId,
                IsAdmin = x.IsAdmin,
                IsCreator=result.CreatedByUserId==x.UserId,
                Name=x.User.FirsName+" "+x.User.LastName
            }).OrderByDescending(x=>x.IsCreator)
            .ThenByDescending(x => x.IsAdmin).ToList();
            return users;

        }
    }
}
