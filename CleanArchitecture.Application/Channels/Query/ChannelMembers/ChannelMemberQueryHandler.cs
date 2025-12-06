using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.ChannelMembers
{
    public sealed class ChannelMemberQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<ChannelMemberQuery, IReadOnlyList<ChannelMemberViewModel>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<IReadOnlyList<ChannelMemberViewModel>>> Handle(ChannelMemberQuery request, CancellationToken cancellationToken)
        {
           var members= await _uow.ChannelMembers.AsNoTracking()
                .Include(X=>X.User)
                .Include(C=>C.Channel.Admins)
                .Where(x=>x.ChannelId== request.ChannelId)
                .Select(x=>new ChannelMemberViewModel
                {
                    Id = x.UserId,
                    Name=x.User.LastName+" "+x.User.FirsName,
                    IsCreator=x.UserId==x.Channel.CreatedByUserId,
                    IsAdmin=x.Channel.Admins.Any(c=>c.UserId==x.UserId)
                }).ToListAsync(cancellationToken);
            return members;
        }
    }
}
