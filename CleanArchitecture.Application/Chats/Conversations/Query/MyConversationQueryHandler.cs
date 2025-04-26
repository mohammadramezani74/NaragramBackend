using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Query
{
    internal sealed class MyConversationQueryHandler(IApplicationUserManager userManager,
        IMapper mapper,IApplicationUnitOfWork uow) : IQueryHandler<MyConversationQuery, ConverSationResponse[]>
    {
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IMapper _mapper = mapper;
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<ConverSationResponse[]>> Handle(MyConversationQuery request, CancellationToken cancellationToken)
        {
            var myId= _userManager.UserId!.Value;
            var query = _uow.Conversation.Include(
                x => x.Users).ThenInclude(x => x.User)
                .Where(x => x.Users.Any(x => x.UserId == myId));
            if (!string.IsNullOrEmpty(request.search))
            {
                query=query.Where(x=>x.Users.Any(x=>x.User.FirsName!.Contains( request.search))|
                x.Users.Any(x => x.User.LastName!.Contains(request.search))|
                x.Users.Any(x => x.User.UserName!.Contains(request.search))|
                 x.Users.Any(x => x.User.Address.City!.Contains(request.search)) 
                );
            }
            var count=await query.CountAsync(cancellationToken);
            var list=await query.OrderByDescending(x=>x.CreateDate).ToListAsync(cancellationToken);
           var mappedObject= _mapper.Map<ConverSationResponse[]>(list);
            return OperationResult.Success(mappedObject, count);
        }
    }
}
